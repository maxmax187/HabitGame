import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import Header from '../components/Header'
import { checkEmail } from '../api/checkEmail'

type FormError = 'not-found' | 'server' | null

function Gateway() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [error, setError] = useState<FormError>(null)
  const [checking, setChecking] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setChecking(true)
    try {
      const resolved = await checkEmail(email)
      if (resolved) {
        navigate(`/${resolved.slug}?email=${encodeURIComponent(resolved.email)}`)
        return
      }
      setError('not-found')
    } catch {
      setError('server')
    } finally {
      setChecking(false)
    }
  }

  return (
    <div className="page">
      <Header />

      <main className="overview-main">
        <h1>Welcome to the Study</h1>

        <section className="intro-card">
          <p>
            Over the next three days, you will be asked to play a short
            game once per day. Enter the email address you registered
            with below to reach your games.
          </p>
          
          <p>
            Contact Chao Zhang, the responsible researcher, at <a href="mailto:c.zhang5@tue.nl">c.zhang5@tue.nl </a> 
            in case you have any questions or run into issues during the study.
          </p>
          <p>
            Thank you for participating!
          </p>
        </section>

        <form className="code-form" onSubmit={handleSubmit}>
          <label htmlFor="participant-email">Email address</label>
          <div className="code-form-row">
            <input
              id="participant-email"
              type="email"
              autoComplete="email"
              spellCheck={false}
              value={email}
              onChange={(event) => {
                setEmail(event.target.value)
                setError(null)
              }}
              placeholder="you@example.com"
            />
            <button type="submit" disabled={checking}>
              {checking ? 'Checking...' : 'Continue'}
            </button>
          </div>
          {error === 'not-found' && (
            <p className="code-form-error">
              That email address wasn&apos;t recognized. Please check that
              you entered the address you registered with and try again.
            </p>
          )}
          {error === 'server' && (
            <p className="code-form-error">
              Something went wrong checking that address. Please try again,
              or contact the researcher if this keeps happening.
            </p>
          )}
        </form>
      </main>

      <footer className="site-footer">
        <p>Eindhoven University of Technology</p>
      </footer>
    </div>
  )
}

export default Gateway
