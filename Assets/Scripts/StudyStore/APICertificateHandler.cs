using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Utils;

public class APICertificateHandler : CertificateHandler
{ 
	X509Certificate2 rootCert;

	public APICertificateHandler()
	{
		TextAsset rootCertAsset = Resources.Load("study-store-ca") as TextAsset;
		rootCert = new X509Certificate2(rootCertAsset.bytes);
	} 

	public APICertificateHandler(X509Certificate2 cert)
	{
		rootCert = cert;
	}

	protected override bool ValidateCertificate(byte[] certificateData)
	{ 
		X509Certificate2 cert = new X509Certificate2(certificateData);

		X509Chain chain = new X509Chain();
		chain.ChainPolicy.ExtraStore.Add(rootCert);

		chain.Build(cert);

		for(int i = 0; i < chain.ChainElements.Count; i ++)
		{ 
			if (chain.ChainElements[i].Certificate.Thumbprint == rootCert.Thumbprint)
			{
				Debug.Log("OK certificate");
				return true;
			}
		}

		Debug.LogError("Invalid certificate");
		return false;
	}
}
