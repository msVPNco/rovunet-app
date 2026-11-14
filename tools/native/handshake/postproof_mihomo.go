package ca

import "github.com/metacubex/tls"

func rovunetRegisterPostHandshake(config *tls.Config) {
	config.RovunetPostHandshake = config.VerifyConnection
}
