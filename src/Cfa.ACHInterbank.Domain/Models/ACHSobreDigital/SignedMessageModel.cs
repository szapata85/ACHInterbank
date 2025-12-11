using System.Xml.Serialization;

namespace Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

[XmlRoot(ElementName = "signerInfo")]
public class SignerInfo
{

    [XmlElement(ElementName = "signatureAlgorithm")]
    public string SignatureAlgorithm { get; set; } = string.Empty;

    [XmlElement(ElementName = "certificateInfo")]
    public CertificateInfo CertificateInfo { get; set; } = new();

    [XmlElement(ElementName = "certificate")]
    public string Certificate { get; set; } = string.Empty;
}

[XmlRoot(ElementName = "signedData")]
public class SignedData
{

    [XmlElement(ElementName = "version")]
    public string Version { get; set; } = string.Empty;

    [XmlElement(ElementName = "signerInfo")]
    public SignerInfo SignerInfo { get; set; } = new();

    [XmlElement(ElementName = "contentInfo")]
    public string ContentInfo { get; set; } = string.Empty;

    [XmlElement(ElementName = "encryptedDigest")]
    public string EncryptedDigest { get; set; } = string.Empty;
}
