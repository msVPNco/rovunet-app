#!/usr/bin/env python3
"""Apply the Android automatic-certificate overlay to the existing pinned core."""
from pathlib import Path
import hashlib, json, os, re, shutil, subprocess, sys

def write_receipt(binary, abi):
    root = Path(__file__).resolve().parents[2]
    def digest(path): return hashlib.sha256(Path(path).read_bytes().replace(b'\r\n', b'\n')).hexdigest()
    receipt = root/'app/.native-receipts/xray'/(abi+'.sha256')
    receipt.parent.mkdir(parents=True, exist_ok=True)
    receipt.write_text('\n'.join(['xray-quic-postproof-v2', digest(__file__),
        digest(Path(__file__).with_name('quic')/'verify_connection.go'),
        digest(Path(__file__).with_name('handshake')/'certificates.go'),
        digest(Path(__file__).with_name('handshake')/'postproof_xray.go'),
        hashlib.sha256(Path(binary).read_bytes()).hexdigest(), '63ca2e0c1d31b89f67700c33842431f541fedd25'])+'\n', encoding='utf-8')

def patch_quic(source, go, environment):
    version = 'v0.61.1-0.20260806010916-184d081eef3e'
    module = 'github.com/apernet/quic-go'
    metadata = json.loads(subprocess.check_output([go, 'mod', 'download', '-json', module+'@'+version], cwd=source, env=environment))
    original = Path(metadata['Dir'])
    target = source / 'rovunet-quic-go'
    if not target.exists():
        shutil.copytree(original, target, copy_function=shutil.copyfile)
    adapter = target / 'internal/handshake/tls_conn_utls.go'
    text = adapter.read_text(encoding='utf-8')
    rejected = '\tif c.VerifyConnection != nil {\n\t\t// Takes a crypto/tls ConnectionState, which uTLS will never produce.\n\t\treturn nil, errors.New("quic: tls.Config.VerifyConnection is not supported with ChromeParrot")\n\t}\n'
    marker = '\trovunetCopyVerifyConnection(c, uc)\n\treturn uc, nil'
    if marker not in text:
        assert text.count(rejected) == 1 and text.count('\treturn uc, nil') == 1
        text = text.replace(rejected, '').replace('\treturn uc, nil', marker)
        adapter.write_text(text, encoding='utf-8')
    helper = Path(__file__).with_name('quic') / 'verify_connection.go'
    (adapter.parent/'rovunet_verify_connection.go').write_bytes(helper.read_bytes())
    subprocess.run([go, 'mod', 'edit', '-replace='+module+'=./rovunet-quic-go'], cwd=source, env=environment, check=True)

def patch_clients(source):
    def edit(relative, marker, old, new):
        path=source/relative;text=path.read_text(encoding='utf-8')
        if marker not in text:
            assert text.count(old)==1, relative
            path.write_text(text.replace(old,new),encoding='utf-8')
    edit('transport/internet/tls/grpc.go','RovunetCloneTLSConfig',
        'cfg := c.config.Clone()', 'cfg := RovunetCloneTLSConfig(c.config)')
    path=source/'transport/internet/tls/grpc.go';text=path.read_text(encoding='utf-8')
    text=text.replace('tc := &grpcUtls{c.Clone(), fingerprint}','tc := &grpcUtls{RovunetCloneTLSConfig(c), fingerprint}')
    path.write_text(text,encoding='utf-8')
    for relative,conn,config,anchor in [
        ('transport/internet/splithttp/dialer.go','conn','gotlsConfig','\t\t\t\tcontext.AfterFunc(conn.Context(), func() { tr.Close(); pktConn.Close() })'),
        ('transport/internet/xdrive/client.go','qconn','gotlsConfig','\t\t\t\tstop := context.AfterFunc(connCtx, func() { qconn.CloseWithError(quic.ApplicationErrorCode(http3.ErrCodeNoError), "") })'),
        ('transport/internet/masque/dialer.go','qconn','gotlsConfig','\tcontext.AfterFunc(qconn.Context(), func() { tr.Close(); pktConn.Close() })')]:
        indent=anchor[:len(anchor)-len(anchor.lstrip('\t'))]
        hook=indent+'if err := tls.RovunetQuicVerified(ctx, '+config+', '+conn+'); err != nil { tr.Close(); pktConn.Close(); return nil,err }\n'
        edit(relative,'tls.RovunetQuicVerified',anchor,hook+anchor)
    edit('transport/internet/finalmask/xdns/resolver_doh.go','tlsNativeConfig :=',
        '\t\ttr = &http3.Transport{\n\t\t\tTLSClientConfig: config.TlsConfig.GetTLSConfig(),',
        '\t\ttlsNativeConfig := config.TlsConfig.GetTLSConfig()\n\t\ttr = &http3.Transport{\n\t\t\tTLSClientConfig: tlsNativeConfig,')
    anchor='\t\t\t\tcontext.AfterFunc(qc.Context(), func() { tr.Close(); conn.Close() })'
    edit('transport/internet/finalmask/xdns/resolver_doh.go','tls.RovunetQuicVerified',anchor,
        '\t\t\t\tif err := tls.RovunetQuicVerified(ctx,tlsNativeConfig,qc); err != nil {tr.Close();conn.Close();return nil,err}\n'+anchor)
    edit('transport/internet/finalmask/xdns/resolver_doh.go','tls.RovunetStandardClient',
        'tlsConn = gotls.Client(conn, tlsConfig)\n\t\t\t\t\terr = tlsConn.(*gotls.Conn).HandshakeContext(ctx)',
        'tlsConn, err = tls.RovunetStandardClient(ctx, conn, tlsConfig)')
    old='\t\t\t\treturn cn, nil\n\t\t\t}\n\t\t}\n\t}\n\n\tif browser_dialer'
    new='\t\t\t\treturn cn, nil\n\t\t\t}\n\t\t} else {\n\t\t\tdialer.NetDialTLSContext = func(ctx context.Context, network, addr string) (net.Conn,error) {\n\t\t\t\traw,err := dialer.NetDial(network,addr); if err != nil {return nil,err}\n\t\t\t\treturn tls.RovunetStandardClient(ctx,raw,tlsConfig)\n\t\t\t}\n\t\t}\n\t}\n\n\tif browser_dialer'
    edit('transport/internet/websocket/dialer.go','tls.RovunetStandardClient',old,new)
    edit('transport/internet/finalmask/realm/http.go','tls.RovunetWrapHTTPTransport',
        'tr.TLSClientConfig = tlsConfig.GetTLSConfig()',
        'tr.TLSClientConfig = tlsConfig.GetTLSConfig(tls.WithOverrideName(host))\n\t\ttls.RovunetWrapHTTPTransport(tr)')
    old='conn, err := quic.DialAddr(context.Background(), s.destination.NetAddr(), tlsConfig.GetTLSConfig(tls.WithNextProto("http/1.1", http2.NextProtoTLS, NextProtoDQ)), quicConfig)'
    new='gotlsConfig := tlsConfig.GetTLSConfig(tls.WithNextProto("http/1.1", http2.NextProtoTLS, NextProtoDQ))\n\tconn, err := quic.DialAddr(context.Background(), s.destination.NetAddr(), gotlsConfig, quicConfig)\n\tif err == nil {err = tls.RovunetQuicVerified(context.Background(),gotlsConfig,conn)}'
    edit('app/dns/nameserver_quic.go','tls.RovunetQuicVerified',old,new)
    client_paths={
        'app/dns/nameserver_quic.go',
        *('transport/internet/'+p for p in ('grpc/dial.go','httpupgrade/dialer.go','finalmask/xdns/resolver_doh.go',
          'finalmask/realm/http.go','kcp/dialer.go','splithttp/dialer.go','hysteria/dialer.go','xdrive/client.go',
          'masque/dialer.go','tcp/dialer.go','websocket/dialer.go'))}
    server_paths={'transport/internet/'+p for p in ('grpc/hub.go','httpupgrade/hub.go','kcp/listener.go',
        'splithttp/hub.go','hysteria/hub.go','masque/hub.go','tcp/hub.go','websocket/hub.go')}
    observed={}
    for path in source.rglob('*.go'):
        relative=path.relative_to(source).as_posix()
        if path.name.endswith('_test.go') or relative.startswith(('rovunet-quic-go/','.git/')): continue
        calls=len(re.findall(r'\.GetTLSConfig\s*\(',path.read_text(encoding='utf-8')))
        if calls: observed[relative]=calls
    assert set(observed)==client_paths|server_paths, 'Unreviewed TLS-config consumer: '+str(set(observed)^(client_paths|server_paths))
    (source/'rovunet-proof-coverage.json').write_text(json.dumps({'clientPaths':sorted(client_paths),
        'serverOnlyPaths':sorted(server_paths),'callCounts':observed,'scope':'Pinned source consumer audit; not runtime acceptance of every protocol'},indent=2)+'\n',encoding='utf-8')

def patch(source, go=None, environment=None):
    source = Path(source)
    patch_quic(source, go or os.environ.get('GO', 'go'), environment)
    shared = Path(__file__).with_name('handshake') / 'certificates.go'
    text = shared.read_text(encoding='utf-8').replace('package ca\n', 'package tls\n', 1)
    text = text.replace('"github.com/metacubex/tls"', '"crypto/tls"')
    text = text.replace('var RovunetCertificateDirectory string', 'var RovunetCertificateDirectory = os.Getenv("ROVUNET_CERTIFICATE_STORE")')
    text = text.replace('var RovunetCertificateEndpoint string', 'var RovunetCertificateEndpoint = os.Getenv("ROVUNET_CERTIFICATE_ENDPOINT")')
    text = text.replace('func rovunetAutomaticCertificate(config *tls.Config, option Option)', 'func rovunetAutomaticCertificate(config *tls.Config)')
    text = text.replace(' || option.Fingerprint != "" || option.NameCertVerify != "" || option.ZeroTrust', ' || config.VerifyConnection != nil')
    assert 'option.' not in text
    target = source / 'transport/internet/tls'
    (target / 'rovunet.go').write_text(text, encoding='utf-8')
    for name in ('publish_unix.go', 'publish_other.go'):
        (target / ('rovunet_' + name)).write_text((shared.parent/name).read_text(encoding='utf-8').replace('package ca\n', 'package tls\n', 1), encoding='utf-8')
    (target/'rovunet_postproof.go').write_bytes((shared.parent/'postproof_xray.go').read_bytes())
    config = target / 'config.go'
    text = config.read_text(encoding='utf-8')
    needle = '\n\treturn config\n}\n\n// Option for building TLS config.'
    replacement = '\n\trovunetAutomaticCertificate(config)\n\treturn config\n}\n\n// Option for building TLS config.'
    if replacement not in text:
        assert text.count(needle) == 1
        config.write_text(text.replace(needle, replacement), encoding='utf-8')
    # uTLS uses a different ConnectionState type. Preserve post-proof verification
    # when copying the standard TLS config to the fingerprinted client.
    config = target / 'tls.go'
    text = config.read_text(encoding='utf-8')
    needle = '\n\treturn config\n}'
    marker = '\n\tif c.VerifyConnection != nil {\n\t\tconfig.VerifyConnection = func(state utls.ConnectionState) error {\n\t\t\treturn c.VerifyConnection(tls.ConnectionState{ServerName: state.ServerName, PeerCertificates: state.PeerCertificates, VerifiedChains: state.VerifiedChains, HandshakeComplete: state.HandshakeComplete, DidResume: state.DidResume})\n\t\t}\n\t}\n\treturn config\n}'
    if marker not in text:
        # The pinned file has one config conversion function returning this value.
        assert text.count(needle) == 1
        config.write_text(text.replace(needle, marker), encoding='utf-8')
    text = config.read_text(encoding='utf-8')
    if 'rovunetConfig *tls.Config' not in text:
        text = text.replace('type Conn struct {\n', 'type Conn struct {\n\trovunetConfig *tls.Config\n')
        text = text.replace('type UConn struct {\n', 'type UConn struct {\n\trovunetConfig *tls.Config\n')
        text = text.replace('return &Conn{Conn: tlsConn}', 'return &Conn{Conn: tlsConn, rovunetConfig: config}', 1)
        text = text.replace('return &UConn{UConn: utlsConn}', 'return &UConn{UConn: utlsConn, rovunetConfig: config}')
        config.write_text(text, encoding='utf-8')
    text = config.read_text(encoding='utf-8')
    if 'rovunetFinalizeOnce sync.Once' not in text:
        text = text.replace('"sync/atomic"', '"sync/atomic"\n\t"sync"')
        text = text.replace('rovunetConfig *tls.Config', 'rovunetConfig *tls.Config\n\trovunetFinalizeOnce sync.Once\n\trovunetFinalizeError error')
        config.write_text(text, encoding='utf-8')
    dialer = source/'transport/internet/hysteria/dialer.go'
    text = dialer.read_text(encoding='utf-8')
    if 'RovunetCertificateVerified' not in text:
        needle = '\t\t\tconn = qc\n\t\t\treturn qc, nil'
        assert text.count(needle) == 1
        hook = '\t\t\tselect {\n\t\t\tcase <-ctx.Done(): qc.CloseWithError(0, ""); return nil, ctx.Err()\n\t\t\tcase <-qc.Context().Done(): return nil, qc.Context().Err()\n\t\t\tcase <-qc.HandshakeComplete():\n\t\t\t}\n\t\t\tif err := tls.RovunetCertificateVerified(c.tlsConfig, qc.ConnectionState().TLS); err != nil {\n\t\t\t\tqc.CloseWithError(0, ""); return nil, err\n\t\t\t}\n'
        dialer.write_text(text.replace(needle, hook+needle), encoding='utf-8')
    (target/'rovunet_test.go').write_bytes((shared.parent/'xray_test.go').read_bytes())
    patch_clients(source)
    (source/'transport/internet/splithttp/rovunet_postproof_test.go').write_bytes((shared.parent/'xhttp_postproof_test.go').read_bytes())

if __name__ == '__main__':
    if sys.argv[1] == '--receipt': write_receipt(sys.argv[2], sys.argv[3])
    else: patch(sys.argv[1])
