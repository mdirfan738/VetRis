namespace VetRIS.Domain.Enums;

/// <summary>DICOM modality codes relevant to VetRIS workflows.</summary>
public enum Modality
{
    CR = 1,  // Computed Radiography
    CT = 2,  // Computed Tomography
    MR = 3,  // Magnetic Resonance
    US = 4,  // Ultrasound
    MG = 5,  // Mammography
    NM = 6,  // Nuclear Medicine
    PT = 7,  // Positron Emission Tomography
    XA = 8,  // X-Ray Angiography
    DX = 9,  // Digital Radiography
    RF = 10, // Radio Fluoroscopy
    OT = 11  // Other
}
