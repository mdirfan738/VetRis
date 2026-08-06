import { useEffect, useState } from 'react'
import { getHealth, listStudies } from '../api/client.js'

export default function Dashboard() {
  const [health, setHealth] = useState(null)
  const [studies, setStudies] = useState([])
  const [error, setError] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let mounted = true

    async function load() {
      try {
        const [healthRes, studiesRes] = await Promise.all([getHealth(), listStudies()])
        if (!mounted) return
        setHealth(healthRes.data)
        setStudies(studiesRes.data)
      } catch (err) {
        if (mounted) setError(err.message)
      } finally {
        if (mounted) setLoading(false)
      }
    }

    load()
    return () => { mounted = false }
  }, [])

  return (
    <div>
      <div className="card">
        <h2>API Health</h2>
        {loading && <p>Checking...</p>}
        {!loading && health && (
          <p className="status-ok">{health.status} — {health.service} (v{health.version})</p>
        )}
        {!loading && error && <p className="status-bad">Unable to reach API: {error}</p>}
      </div>

      <div className="card">
        <h2>Studies</h2>
        {studies.length === 0 && !loading && <p>No studies yet. Try the Upload DICOM page.</p>}
        {studies.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>Id</th>
                <th>Patient Id</th>
                <th>Study Date</th>
                <th>Status</th>
                <th>S3 Key</th>
              </tr>
            </thead>
            <tbody>
              {studies.map((s) => (
                <tr key={s.id}>
                  <td>{s.id}</td>
                  <td>{s.patientId}</td>
                  <td>{s.studyDate}</td>
                  <td>{s.status}</td>
                  <td>{s.s3Key}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
