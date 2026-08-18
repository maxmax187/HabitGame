import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import Header from '../components/Header'
import { resolveCode } from '../data/codebook'

function Gateway() {
  const navigate = useNavigate()
  const [code, setCode] = useState('')
  const [error, setError] = useState(false)

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const slug = resolveCode(code)
    if (slug) {
      navigate(`/${slug}`)
    } else {
      setError(true)
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
            game once per day. Enter the access code you were given below
            to reach your games.
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
          <label htmlFor="access-code">Access code</label>
          <div className="code-form-row">
            <input
              id="access-code"
              type="text"
              autoComplete="off"
              autoCapitalize="characters"
              spellCheck={false}
              value={code}
              onChange={(event) => {
                setCode(event.target.value)
                setError(false)
              }}
              placeholder="e.g. AB12-3456"
            />
            <button type="submit">Continue</button>
          </div>
          {error && (
            <p className="code-form-error">
              That code wasn&apos;t recognized. Please check the code you
              were given and try again.
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
