using System.Xml.Serialization;

namespace Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

[XmlRoot(ElementName = "signerInfo")]
public class SignerInfo
{

    [XmlElement(ElementName = "signatureAlgorithm")]
    public string SignatureAlgorithm { get; set; }

    [XmlElement(ElementName = "certificateInfo")]
    public CertificateInfo CertificateInfo { get; set; }

    [XmlElement(ElementName = "certificate")]
    public string Certificate { get; set; }
}

[XmlRoot(ElementName = "signedData")]
public class SignedData
{

    [XmlElement(ElementName = "version")]
    public string Version { get; set; }

    [XmlElement(ElementName = "signerInfo")]
    public SignerInfo SignerInfo { get; set; }

    [XmlElement(ElementName = "contentInfo")]
    public string ContentInfo { get; set; }

    [XmlElement(ElementName = "encryptedDigest")]
    public string EncryptedDigest { get; set; }
}
