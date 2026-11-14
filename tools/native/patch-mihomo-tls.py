"""Keep first-use certificate publication after actual TLS ownership proof."""
from pathlib import Path
import json, os, shutil, subprocess

def patch(source, go=None, environment=None):
    source=Path(source);go=go or os.environ.get('GO','go')
    module='github.com/metacubex/tls';version='v0.1.9'
    metadata=json.loads(subprocess.check_output([go,'mod','download','-json',module+'@'+version],cwd=source,env=environment))
    target=source/'rovunet-tls'
    if not target.exists(): shutil.copytree(metadata['Dir'],target,copy_function=shutil.copyfile)
    config=target/'common.go';text=config.read_text(encoding='utf-8')
    if 'RovunetPostHandshake func(ConnectionState) error' not in text:
        needle='VerifyConnection func(ConnectionState) error'
        assert text.count(needle)==1
        text=text.replace(needle,needle+'\n\t// Rovunet: publish identity only after a complete verified handshake.\n\tRovunetPostHandshake func(ConnectionState) error')
        needle='VerifyConnection:                    c.VerifyConnection,'
        assert text.count(needle)==1
        config.write_text(text.replace(needle,needle+'\n\t\tRovunetPostHandshake: c.RovunetPostHandshake,'),encoding='utf-8')
    conn=target/'conn.go';text=conn.read_text(encoding='utf-8')
    # A parallel Read/Write must not use the TLS-complete fast path while the
    # post-proof CAS is still pending on the handshaking goroutine.
    if 'rovunetPostProofComplete atomic.Bool' not in text:
        text=text.replace('type Conn struct {','type Conn struct {\n\trovunetPostProofComplete atomic.Bool',1)
        start=text.index('func (c *Conn) handshakeContext(')
        end=text.index('\n// ConnectionState returns',start)
        method=text[start:end]
        needle='if c.isHandshakeComplete.Load() {\n\t\treturn nil\n\t}'
        assert method.count(needle)==2
        method=method.replace(needle,'if c.isHandshakeComplete.Load() && (c.config.RovunetPostHandshake == nil || c.rovunetPostProofComplete.Load()) {\n\t\treturn nil\n\t}')
        text=text[:start]+method+text[end:]
    marker='\n\t// Rovunet post-proof publication; no app data is released before this.\n'
    if marker not in text:
        needle='\n\tif c.quic != nil {\n\t\tif c.handshakeErr == nil {'
        assert text.count(needle)==1
        hook=marker+'\tif c.handshakeErr == nil && c.config.RovunetPostHandshake != nil {\n\t\tif err := c.config.RovunetPostHandshake(c.connectionStateLocked()); err != nil {\n\t\t\tc.handshakeErr = err\n\t\t\tc.isHandshakeComplete.Store(false)\n\t\t\tc.sendAlert(alertBadCertificate)\n\t\t}\n\t}\n'
        text=text.replace(needle,hook+needle)
    if '\n\tif c.handshakeErr == nil { c.rovunetPostProofComplete.Store(true) }\n' not in text:
        needle='\n\tif c.quic != nil {\n\t\tif c.handshakeErr == nil {'
        assert text.count(needle)==1
        text=text.replace(needle,'\n\tif c.handshakeErr == nil { c.rovunetPostProofComplete.Store(true) }\n'+needle)
    conn.write_text(text,encoding='utf-8')
    subprocess.run([go,'mod','edit','-replace='+module+'=./rovunet-tls'],cwd=source,env=environment,check=True)
