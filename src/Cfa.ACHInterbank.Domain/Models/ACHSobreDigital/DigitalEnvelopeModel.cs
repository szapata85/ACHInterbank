using System.Xml.Serialization;

namespace Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

//internal class DigitalEnvelopeModel
//{
//}

[XmlRoot(ElementName = "certificateInfo")]
public class CertificateInfo
{

    [XmlElement(ElementName = "issuer")]
    public string Issuer { get; set; } = string.Empty;

    [XmlElement(ElementName = "serial")]
    public string Serial { get; set; } = string.Empty;
}

[XmlRoot(ElementName = "recipientInfo")]
public class RecipientInfo
{

    [XmlElement(ElementName = "certificateInfo")]
    public CertificateInfo CertificateInfo { get; set; } = new();

    [XmlElement(ElementName = "keyEncryptionAlgorithm")]
    public string KeyEncryptionAlgorithm { get; set; } = string.Empty;

    [XmlElement(ElementName = "encryptedKey")]
    public string EncryptedKey { get; set; } = string.Empty;
}

[XmlRoot(ElementName = "encryptedContentInfo")]
public class EncryptedContentInfo
{

    [XmlElement(ElementName = "contentType")]
    public string ContentType { get; set; } = string.Empty;

    [XmlElement(ElementName = "contentEncryptionAlgorithm")]
    public string ContentEncryptionAlgorithm { get; set; } = string.Empty;

    [XmlElement(ElementName = "encryptedContent")]
    public string EncryptedContent { get; set; } = string.Empty;
}

[XmlRoot(ElementName = "envelope")]
public class DigitalEnvelopeModel
{

    [XmlElement(ElementName = "version")]
    public int Version { get; set; }

    [XmlElement(ElementName = "identifier")]
    public string Identifier { get; set; } = string.Empty;

    [XmlElement(ElementName = "timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [XmlElement(ElementName = "recipientInfo")]
    public RecipientInfo RecipientInfo { get; set; } = new();

    [XmlElement(ElementName = "encryptedContentInfo")]
    public EncryptedContentInfo EncryptedContentInfo { get; set; } = new();
}
