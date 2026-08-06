import { Routes, Route, Link, Navigate } from 'react-router-dom'
import Login from './pages/Login.jsx'
import Dashboard from './pages/Dashboard.jsx'
import UploadDicom from './pages/UploadDicom.jsx'

// NOTE: This is a placeholder scaffold. There is no real auth/session
// management here — replace with your identity provider (e.g. Cognito,
// Auth0, or an internal IdP) before handling real PHI.
export default function App() {
  return (
    <div className="app-shell">
      <header className="app-header">
        <h1>VetRIS Teleradiology (Scaffold)</h1>
        <nav>
          <Link to="/">Login</Link>
          <Link to="/dashboard">Dashboard</Link>
          <Link to="/upload">Upload DICOM</Link>
        </nav>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<Login />} />
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/upload" element={<UploadDicom />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  )
}
