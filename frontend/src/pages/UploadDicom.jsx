import { useState } from 'react'
import { uploadStudy } from '../api/client.js'

export default function UploadDicom() {
  const [file, setFile] = useState(null)
  const [patientId, setPatientId] = useState('')
  const [progress, setProgress] = useState(0)
  const [result, setResult] = useState(null)
  const [error, setError] = useState(null)
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    if (!file || !patientId) return

    setSubmitting(true)
    setError(null)
    setResult(null)

    const formData = new FormData()
    formData.append('file', file)
    formData.append('patientId', patientId)

    try {
      const res = await uploadStudy(formData, (evt) => {
        if (evt.total) setProgress(Math.round((evt.loaded * 100) / evt.total))
      })
      setResult(res.data)
    } catch (err) {
      setError(err.response?.data?.message || err.message)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="card" style={{ maxWidth: 480 }}>
      <h2>Upload DICOM Study</h2>
      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
        <input
          placeholder="Patient Id"
          value={patientId}
          onChange={(e) => setPatientId(e.target.value)}
        />
        <input
          type="file"
          accept=".dcm,application/dicom"
          onChange={(e) => setFile(e.target.files?.[0] ?? null)}
        />
        <button type="submit" disabled={submitting || !file || !patientId}>
          {submitting ? `Uploading ${progress}%` : 'Upload'}
        </button>
      </form>

      {result && (
        <div style={{ marginTop: 16 }}>
          <p className="status-ok">Uploaded. Study Id: {result.id}</p>
          <pre style={{ fontSize: 12, whiteSpace: 'pre-wrap' }}>{JSON.stringify(result, null, 2)}</pre>
        </div>
      )}
      {error && <p className="status-bad">Upload failed: {error}</p>}

      <p style={{ fontSize: 12, color: '#6b7280', marginTop: 12 }}>
        File is sent to the backend, which streams it to the S3 "active" bucket
        (server-side encrypted with KMS) and writes a Study record with the
        resulting S3 key.
      </p>
    </div>
  )
}
