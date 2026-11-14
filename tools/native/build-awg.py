#!/usr/bin/env python3
"""Build the constrained AWG child from pinned, GPL-3.0 Mihomo sources."""
import hashlib,importlib.util,os,pathlib,shutil,subprocess,tempfile
ROOT=pathlib.Path(__file__).resolve().parents[2]
PIN='3f27ef39f8c96bbfbc8b0b9bb10f5c9ef60149d8'
def run(args,**kw):subprocess.run([str(x) for x in args],check=True,**kw)
def record_probe_mapping(abi,binary):
 # An old prebuilt child must never be packaged with the new Kotlin capability.
 sources=['tools/native/awg/probe.go','tools/native/handshake/certificates.go','tools/native/handshake/postproof_mihomo.go','tools/native/patch-mihomo-tls.py']
 hashes=[hashlib.sha256((ROOT/p).read_bytes().replace(b'\r\n',b'\n')).hexdigest() for p in sources]
 receipt=ROOT/'app/.native-receipts/awg'/f'{abi}.sha256'
 receipt.parent.mkdir(parents=True,exist_ok=True)
 receipt.write_text('\n'.join(['probe-mapping-v3-postproof',*hashes,hashlib.sha256(binary.read_bytes()).hexdigest(),PIN])+'\n',encoding='utf-8')
with tempfile.TemporaryDirectory(prefix='rovunet-awg-build-') as tmp:
 source=pathlib.Path(tmp)/'mihomo'
 run(['git','clone','--filter=blob:none','--no-checkout','https://github.com/MetaCubeX/mihomo.git',source]);run(['git','checkout',PIN],cwd=source)
 # Backport upstream c65570ed0924a7e48a48160463c68015b9a84ca8 / PR3359.
 # Preserve the reviewed core pin; domain-length byte arithmetic must not crash the child.
 patch=ROOT/'tools/native/mihomo-socks-domain.patch'
 run(['git','apply','--check',patch],cwd=source)
 run(['git','apply',patch],cwd=source)
 for name,target in [('outbound.go','adapter/outbound/rovunet_handshake.go'),('tuic_capability_test.go','adapter/outbound/rovunet_tuic_test.go'),('certificates.go','component/ca/rovunet.go'),('postproof_mihomo.go','component/ca/rovunet_postproof.go'),('mihomo_test.go','component/ca/rovunet_postproof_test.go'),('publish_unix.go','component/ca/rovunet_publish_unix.go'),('publish_other.go','component/ca/rovunet_publish_other.go')]:
  shutil.copy2(ROOT/'tools/native/handshake'/name,source/target)
 ca_config=source/'component/ca/config.go'
 ca_text=ca_config.read_text()
 needle='\treturn tlsConfig, nil\n'
 assert ca_text.count(needle)==1
 ca_config.write_text(ca_text.replace(needle,'\trovunetAutomaticCertificate(tlsConfig, opt)\n'+needle))
 dest=source/'cmd/rovunet-awg';shutil.copytree(ROOT/'tools/native/awg',dest)
 go=os.environ.get('GO','go')
 spec=importlib.util.spec_from_file_location('tls_postproof',ROOT/'tools/native/patch-mihomo-tls.py')
 module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);module.patch(source,go)
 run([go,'test','./component/ca','-run','TestRovunet','-count=1'],cwd=source)
 run([go,'test','./adapter/outbound','-run','TestRovunetTuic','-count=1'],cwd=source)
 run([go,'test','./adapter/inbound','-run','TestParseSocksAddr(DomainLengths|FQDN)$','-count=1'],cwd=source)
 if os.environ.get('ROVUNET_AWG_HOST'):
  run([go,'test','./cmd/rovunet-awg'],cwd=source)
  run([go,'build','-trimpath','-ldflags=-s -w -checklinkname=0','-o',os.environ['ROVUNET_AWG_HOST'],'./cmd/rovunet-awg'],cwd=source)
 else:
  ndk=pathlib.Path(os.environ['ANDROID_NDK_HOME'])
  host='windows-x86_64' if os.name=='nt' else 'linux-x86_64'
  for abi,arch,target in [('arm64-v8a','arm64','aarch64-linux-android24'),('armeabi-v7a','arm','armv7a-linux-androideabi24'),('x86_64','amd64','x86_64-linux-android24')]:
   compiler=ndk/'toolchains/llvm/prebuilt'/host/'bin'/('clang.exe' if os.name=='nt' else target+'-clang')
   cc=str(compiler)+(' --target='+target if os.name=='nt' else '')
   env=os.environ.copy();env.update(GOOS='android',GOARCH=arch,GOARM='7',CGO_ENABLED='1',CC=cc)
   out=ROOT/'app/src/main/jniLibs'/abi/'librovunet_awg.so';out.parent.mkdir(parents=True,exist_ok=True)
   run([go,'build','-trimpath','-buildmode=pie','-ldflags=-s -w -checklinkname=0','-o',out,'./cmd/rovunet-awg'],cwd=source,env=env)
   record_probe_mapping(abi,out)
  fixture=ROOT/'app/build/rovunet-test-fixtures/udp-protocols'
  fixture.parent.mkdir(parents=True,exist_ok=True)
  run([go,'test','-c','-o',fixture,'./cmd/rovunet-awg'],cwd=source)
