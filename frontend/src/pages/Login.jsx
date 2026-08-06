import { useState } from 'react'
import { useNavigate } from 'react-router-dom'

// PLACEHOLDER LOGIN — no real authentication is performed here.
// Replace with a real identity provider (Cognito, Auth0, Okta, etc.)
// before this touches any real patient data (PHI).
export default function Login() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const navigate = useNavigate()

  const handleSubmit = (e) => {
    e.preventDefault()
    // TODO: replace with real auth call
    navigate('/dashboard')
  }

  return (
    <div className="card" style={{ maxWidth: 360 }}>
      <h2>Sign in</h2>
      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
        <input
          placeholder="Username"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
        />
        <input
          placeholder="Password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
        <button type="submit">Sign in</button>
      </form>
      <p style={{ fontSize: 12, color: '#6b7280', marginTop: 12 }}>
        Scaffold only — this form does not authenticate against a real backend.
      </p>
    </div>
  )
}
