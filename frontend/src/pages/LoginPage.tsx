import { useEffect, useState, type FormEvent } from 'react'
import { Building2, KeyRound, Loader2, ShieldCheck } from 'lucide-react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { getDemoCredentials } from '../api/propCareApi'
import { useAuth } from '../context/AuthContext'
import type { DemoCredentialResponse } from '../types/api'

const fallbackCredentials: DemoCredentialResponse[] = [
  {
    role: 'Admin / Owner',
    email: 'admin@propcare.demo',
    password: 'PropCare@Admin123',
    purpose: 'Portfolio and user oversight access.',
  },
  {
    role: 'Property Manager',
    email: 'manager@propcare.demo',
    password: 'PropCare@Manager123',
    purpose: 'Property and maintenance workflow access.',
  },
  {
    role: 'Tenant - Sara',
    email: 'tenant@propcare.demo',
    password: 'PropCare@Tenant123',
    purpose: 'Tenant service portal access.',
  },
  {
    role: 'Tenant - Imran',
    email: 'imran@propcare.demo',
    password: 'PropCare@Imran123',
    purpose: 'Tenant service portal access.',
  },
  {
    role: 'Maintenance Staff',
    email: 'staff@propcare.demo',
    password: 'PropCare@Staff123',
    purpose: 'Assigned work access.',
  },
]

type LocationState = {
  from?: {
    pathname?: string
  }
}

function LoginPage() {
  const { isAuthenticated, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [credentials, setCredentials] = useState(fallbackCredentials)
  const [isLoadingCredentials, setIsLoadingCredentials] = useState(true)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState('')
  const from = (location.state as LocationState | null)?.from?.pathname ?? '/'

  useEffect(() => {
    let isMounted = true

    async function loadCredentials() {
      try {
        const data = await getDemoCredentials()
        if (isMounted && data.length > 0) {
          setCredentials(data)
        }
      } finally {
        if (isMounted) {
          setIsLoadingCredentials(false)
        }
      }
    }

    void loadCredentials()

    return () => {
      isMounted = false
    }
  }, [])

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  function useCredential(credential: DemoCredentialResponse) {
    setEmail(credential.email)
    setPassword(credential.password)
    setError('')
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)
    setError('')

    try {
      await login(email, password)
      navigate(from, { replace: true })
    } catch (loginError) {
      setError(
        loginError instanceof Error
          ? loginError.message
          : 'Sign in failed. Check the email and password, then try again.',
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="min-h-screen bg-slate-100 px-4 py-8 text-slate-950 sm:px-6 lg:px-8">
      <div className="mx-auto grid min-h-[calc(100vh-4rem)] w-full max-w-5xl items-center gap-6 lg:grid-cols-[0.9fr_1.1fr]">
        <section className="space-y-6">
          <div className="flex items-center gap-3">
            <div className="flex size-12 items-center justify-center rounded-lg bg-cyan-700 text-white shadow-sm">
              <Building2 className="size-6" aria-hidden="true" />
            </div>
            <div>
              <h1 className="text-2xl font-semibold text-slate-950">
                PropCare Cloud
              </h1>
              <p className="text-sm text-slate-500">
                Property Maintenance Portal
              </p>
            </div>
          </div>

          <div>
            <p className="text-sm font-semibold text-cyan-700">
              Secure account access
            </p>
            <h2 className="mt-3 text-4xl font-semibold tracking-normal text-slate-950">
              Sign in to manage maintenance work with confidence.
            </h2>
            <p className="mt-4 max-w-xl text-base leading-7 text-slate-600">
              Access your property maintenance dashboard.
            </p>
          </div>
        </section>

        <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm sm:p-8">
          <div className="flex items-start justify-between gap-4">
            <div>
              <h2 className="text-2xl font-semibold text-slate-950">
                Welcome back
              </h2>
              <p className="mt-2 text-sm text-slate-500">
                Enter your account details to continue.
              </p>
            </div>
            <div className="flex size-11 items-center justify-center rounded-md bg-emerald-50 text-emerald-700">
              <ShieldCheck className="size-5" aria-hidden="true" />
            </div>
          </div>

          <form className="mt-7 space-y-5" onSubmit={handleSubmit}>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">Email</span>
              <input
                type="email"
                required
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              />
            </label>

            <label className="block">
              <span className="text-sm font-medium text-slate-700">Password</span>
              <input
                type="password"
                required
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              />
            </label>

            {error && (
              <div className="rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-800">
                {error}
              </div>
            )}

            <button
              type="submit"
              disabled={isSubmitting}
              className="inline-flex w-full items-center justify-center gap-2 rounded-md bg-cyan-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-cyan-800 disabled:cursor-not-allowed disabled:bg-slate-400"
            >
              {isSubmitting ? (
                <Loader2 className="size-4 animate-spin" aria-hidden="true" />
              ) : (
                <KeyRound className="size-4" aria-hidden="true" />
              )}
              Sign in
            </button>
          </form>

          <details className="mt-6 rounded-lg border border-slate-200 bg-slate-50 p-4">
            <summary className="cursor-pointer text-sm font-semibold text-slate-700">
              Use provided assessment credentials
            </summary>
            <div className="mt-4 grid gap-2">
              {credentials.map((credential) => (
                <button
                  key={credential.email}
                  type="button"
                  onClick={() => useCredential(credential)}
                  className="rounded-md border border-slate-200 bg-white p-3 text-left text-sm transition hover:border-cyan-300 hover:bg-cyan-50"
                >
                  <span className="font-semibold text-slate-950">
                    {credential.role}
                  </span>
                  <span className="mt-1 block text-slate-600">
                    {credential.email}
                  </span>
                </button>
              ))}
            </div>
            <p className="mt-3 text-xs text-slate-500">
              {isLoadingCredentials
                ? 'Loading credential options...'
                : 'Select a role to fill the form for local assessment.'}
            </p>
          </details>
        </section>
      </div>
    </main>
  )
}

export default LoginPage
