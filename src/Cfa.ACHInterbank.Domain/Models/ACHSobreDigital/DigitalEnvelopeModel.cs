using System.Xml.Serialization;

namespace Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

//internal class DigitalEnvelopeModel
//{
//}

[XmlRoot(ElementName = "certificateInfo")]
public class CertificateInfo
{

    [XmlElement(ElementName = "issuer")]
    public string Issuer { get; set; }

    [XmlElement(ElementName = "serial")]
    public string Serial { get; set; }
}

[XmlRoot(ElementName = "recipientInfo")]
public class RecipientInfo
{

    [XmlElement(ElementName = "certificateInfo")]
    public CertificateInfo CertificateInfo { get; set; }

    [XmlElement(ElementName = "keyEncryptionAlgorithm")]
    public string KeyEncryptionAlgorithm { get; set; }

    [XmlElement(ElementName = "encryptedKey")]
    public string EncryptedKey { get; set; }
}

[XmlRoot(ElementName = "encryptedContentInfo")]
public class EncryptedContentInfo
{

    [XmlElement(ElementName = "contentType")]
    public string ContentType { get; set; }

    [XmlElement(ElementName = "contentEncryptionAlgorithm")]
    public string ContentEncryptionAlgorithm { get; set; }

    [XmlElement(ElementName = "encryptedContent")]
    public string EncryptedContent { get; set; }
}

[XmlRoot(ElementName = "envelope")]
public class DigitalEnvelopeModel
{

    [XmlElement(ElementName = "version")]
    public int Version { get; set; }

    [XmlElement(ElementName = "identifier")]
    public string Identifier { get; set; }

    [XmlElement(ElementName = "timestamp")]
    public string Timestamp { get; set; }

    [XmlElement(ElementName = "recipientInfo")]
    public RecipientInfo RecipientInfo { get; set; }

    [XmlElement(ElementName = "encryptedContentInfo")]
    public EncryptedContentInfo EncryptedContentInfo { get; set; }
}
