package splithttp_test

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"os"
	"path/filepath"
	"sync/atomic"
	"testing"
	"time"
	"github.com/xtls/xray-core/common/net"
	"github.com/xtls/xray-core/common/protocol/tls/cert"
	"github.com/xtls/xray-core/testing/servers/udp"
	"github.com/xtls/xray-core/transport/internet"
	. "github.com/xtls/xray-core/transport/internet/splithttp"
	"github.com/xtls/xray-core/transport/internet/stat"
	"github.com/xtls/xray-core/transport/internet/tls"
)

func TestRovunetXhttpH3PublishesProvedAAndRejectsChangedB(t *testing.T) {
	first,_:=cert.MustGenerate(nil,cert.CommonName("localhost"),cert.DNSNames("localhost"))
	second,_:=cert.MustGenerate(nil,cert.CommonName("localhost"),cert.DNSNames("localhost"))
	port:=udp.PickPort()
	oldDir,oldPeer:=tls.RovunetCertificateDirectory,tls.RovunetCertificateEndpoint
	defer func(){tls.RovunetCertificateDirectory,tls.RovunetCertificateEndpoint=oldDir,oldPeer}()
	directory:=t.TempDir()
	tls.RovunetCertificateDirectory,tls.RovunetCertificateEndpoint=directory,"localhost:"+port.String()
	var accepted atomic.Int32
	listen:=func(certificate *cert.Certificate)*Listener {
		// The peer is a fixture server; only the client receives our trust policy.
		tls.RovunetCertificateDirectory=""
		settings:=&internet.MemoryStreamConfig{ProtocolName:"splithttp",ProtocolSettings:&Config{Path:"/proof"},
			SecurityType:"tls",SecuritySettings:&tls.Config{ServerName:"localhost",NextProtocol:[]string{"h3"},Certificate:[]*tls.Certificate{tls.ParseCertificate(certificate)}}}
		server,err:=ListenXH(context.Background(),net.LocalHostIP,port,settings,func(connection stat.Connection){
			accepted.Add(1)
			go func(){defer connection.Close();io.Copy(connection,connection)}()
		})
		tls.RovunetCertificateDirectory=directory
		if err!=nil{t.Fatal("XHTTP/h3 fixture failed to bind",err)}
		return server.(*Listener)
	}
	exchange:=func()error {
		ctx,cancel:=context.WithTimeout(context.Background(),3*time.Second);defer cancel()
		settings:=&internet.MemoryStreamConfig{ProtocolName:"splithttp",ProtocolSettings:&Config{Path:"/proof"},
			SecurityType:"tls",SecuritySettings:&tls.Config{ServerName:"localhost",NextProtocol:[]string{"h3"}}}
		connection,err:=Dial(ctx,net.UDPDestination(net.DomainAddress("localhost"),port),settings)
		if err!=nil{return err};defer connection.Close()
		timer:=time.AfterFunc(3*time.Second,func(){connection.Close()});defer timer.Stop()
		payload:=bytes.Repeat([]byte("synthetic-XHTTP-h3"),128)
		if _,err=connection.Write(payload);err!=nil{return err}
		got:=make([]byte,len(payload));if _,err=io.ReadFull(connection,got);err!=nil{return err}
		if !bytes.Equal(got,payload){return fmt.Errorf("XHTTP/h3 encrypted echo changed")}
		return nil
	}
	server:=listen(first)
	if err:=exchange();err!=nil{server.Close();t.Fatal("XHTTP/h3 A relay failed",err)}
	files,_:=filepath.Glob(filepath.Join(directory,"*.json"));if len(files)!=1{server.Close();t.Fatal("XHTTP/h3 A did not create exactly one proved pin")}
	stored,err:=os.ReadFile(files[0]);if err!=nil{t.Fatal(err)}
	var identity struct{Certificate []byte};if json.Unmarshal(stored,&identity)!=nil || !bytes.Equal(identity.Certificate,first.Certificate){t.Fatal("XHTTP/h3 did not store A's actual certificate")}
	server.Close();time.Sleep(150*time.Millisecond)
	before:=accepted.Load();server=listen(second);defer server.Close()
	if err:=exchange();err==nil{t.Fatal("XHTTP/h3 accepted changed identity B")}
	if accepted.Load()!=before{t.Fatal("XHTTP/h3 sent an application stream before rejecting B")}
	after,_:=os.ReadFile(files[0]);if !bytes.Equal(stored,after){t.Fatal("XHTTP/h3 B overwrote the A pin")}
}
