import axios from 'axios'
import { useState, type FormEvent } from 'react'
import {
  ArrowLeft,
  Building2,
  CheckCircle2,
  Loader2,
  LockKeyhole,
  Send,
  ShieldCheck,
} from 'lucide-react'
import { Link } from 'react-router-dom'
import { submitTenantRegistration } from '../api/propCareApi'

const initialForm = {
  firstName: '',
  lastName: '',
  email: '',
  phoneNumber: '',
  requestedPropertyOrUnit: '',
  note: '',
}

function getErrorMessage(error: unknown) {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { message?: string } | undefined
    return (
      data?.message ??
      'Registration request could not be submitted. Please review the details and try again.'
    )
  }

  return 'Registration request could not be submitted. Please try again.'
}

function cleanOptional(value: string) {
  return value.trim() || null
}

function TenantRegistrationPage() {
  const [form, setForm] = useState(initialForm)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [successMessage, setSuccessMessage] = useState('')
  const [error, setError] = useState('')

  function updateForm(field: keyof typeof initialForm, value: string) {
    setForm((current) => ({ ...current, [field]: value }))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)
    setError('')
    setSuccessMessage('')

    try {
      await submitTenantRegistration({
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        email: form.email.trim(),
        phoneNumber: cleanOptional(form.phoneNumber),
        requestedPropertyOrUnit: cleanOptional(form.requestedPropertyOrUnit),
        note: cleanOptional(form.note),
      })
      setForm(initialForm)
      setSuccessMessage(
        'Your registration request has been submitted and is pending approval.',
      )
    } catch (submitError) {
      setError(getErrorMessage(submitError))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="premium-auth-shell blueprint-surface min-h-screen px-4 py-8 text-slate-950 sm:px-6 lg:px-8">
      <div className="mx-auto grid min-h-[calc(100vh-4rem)] w-full max-w-6xl items-center gap-6 lg:grid-cols-[0.9fr_1fr]">
        <section className="space-y-6">
          <Link
            to="/welcome"
            className="inline-flex items-center gap-2 text-sm font-semibold text-cyan-700 hover:text-cyan-800"
          >
            <ArrowLeft className="size-4" aria-hidden="true" />
            Back
          </Link>

          <div className="flex items-center gap-3">
            <div className="flex size-12 items-center justify-center rounded-xl bg-cyan-700 text-white shadow-lg shadow-cyan-900/20">
              <Building2 className="size-6" aria-hidden="true" />
            </div>
            <div>
              <h1 className="text-2xl font-semibold text-slate-950">
                PropCare Cloud
              </h1>
              <p className="text-sm text-slate-500">
                Tenant Access Request
              </p>
            </div>
          </div>

          <div>
            <p className="premium-eyebrow">Tenant portal registration</p>
            <h2 className="mt-3 text-4xl font-semibold tracking-normal text-slate-950">
              Request secure access to your tenant portal.
            </h2>
            <p className="mt-4 max-w-xl text-base leading-7 text-slate-600">
              Submit your details for review by the property team. Approved
              tenants receive portal access for their assigned rental unit.
            </p>
          </div>

          <div className="grid gap-3">
            <RegistrationCue
              icon={ShieldCheck}
              title="Approval controlled"
              text="Requests stay pending until an authorized admin or manager reviews them."
            />
            <RegistrationCue
              icon={LockKeyhole}
              title="Tenant-only scope"
              text="Approved accounts use the existing tenant privacy and unit isolation rules."
            />
            <RegistrationCue
              icon={CheckCircle2}
              title="Unit assignment"
              text="Access is connected to a verified available rental unit during approval."
            />
          </div>
        </section>

        <section className="premium-hero-card p-6 sm:p-8">
          <div>
            <p className="text-sm font-semibold text-cyan-700">
              Request Tenant Portal Access
            </p>
            <h2 className="mt-2 text-2xl font-semibold text-slate-950">
              Registration request
            </h2>
            <p className="mt-2 text-sm leading-6 text-slate-500">
              Required fields are reviewed before account activation.
            </p>
          </div>

          {successMessage && (
            <div className="mt-5 rounded-lg border border-emerald-200 bg-emerald-50 p-4 text-sm font-medium text-emerald-900">
              {successMessage}
            </div>
          )}

          {error && (
            <div className="mt-5 rounded-lg border border-rose-200 bg-rose-50 p-4 text-sm font-medium text-rose-900">
              {error}
            </div>
          )}

          <form className="mt-6 space-y-4" onSubmit={handleSubmit}>
            <div className="grid gap-4 sm:grid-cols-2">
              <label className="block">
                <span className="text-sm font-medium text-slate-700">
                  First name
                </span>
                <input
                  required
                  maxLength={100}
                  value={form.firstName}
                  onChange={(event) => updateForm('firstName', event.target.value)}
                  className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                />
              </label>
              <label className="block">
                <span className="text-sm font-medium text-slate-700">
                  Last name
                </span>
                <input
                  required
                  maxLength={100}
                  value={form.lastName}
                  onChange={(event) => updateForm('lastName', event.target.value)}
                  className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                />
              </label>
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <label className="block">
                <span className="text-sm font-medium text-slate-700">Email</span>
                <input
                  required
                  type="email"
                  maxLength={256}
                  value={form.email}
                  onChange={(event) => updateForm('email', event.target.value)}
                  className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                />
              </label>
              <label className="block">
                <span className="text-sm font-medium text-slate-700">
                  Phone number
                </span>
                <input
                  maxLength={30}
                  value={form.phoneNumber}
                  onChange={(event) => updateForm('phoneNumber', event.target.value)}
                  className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                />
              </label>
            </div>

            <label className="block">
              <span className="text-sm font-medium text-slate-700">
                Requested property or unit
              </span>
              <input
                maxLength={250}
                value={form.requestedPropertyOrUnit}
                onChange={(event) =>
                  updateForm('requestedPropertyOrUnit', event.target.value)
                }
                className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              />
            </label>

            <label className="block">
              <span className="text-sm font-medium text-slate-700">
                Short note or reason
              </span>
              <textarea
                rows={4}
                maxLength={1000}
                value={form.note}
                onChange={(event) => updateForm('note', event.target.value)}
                className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              />
            </label>

            <button
              type="submit"
              disabled={isSubmitting}
              className="inline-flex w-full items-center justify-center gap-2 rounded-md bg-cyan-700 px-4 py-2.5 text-sm font-semibold text-white shadow-lg shadow-cyan-900/20 transition hover:-translate-y-0.5 hover:bg-cyan-800 disabled:cursor-not-allowed disabled:bg-slate-400 disabled:shadow-none"
            >
              {isSubmitting ? (
                <Loader2 className="size-4 animate-spin" aria-hidden="true" />
              ) : (
                <Send className="size-4" aria-hidden="true" />
              )}
              Submit request
            </button>
          </form>

          <div className="mt-5 flex flex-wrap items-center gap-3 text-sm font-semibold">
            <Link to="/login" className="text-cyan-700 hover:text-cyan-800">
              Sign in instead
            </Link>
            <span className="text-slate-300">/</span>
            <Link to="/welcome" className="text-slate-600 hover:text-slate-900">
              Back to welcome
            </Link>
          </div>
        </section>
      </div>
    </main>
  )
}

function RegistrationCue({
  icon: Icon,
  title,
  text,
}: {
  icon: typeof ShieldCheck
  title: string
  text: string
}) {
  return (
    <div className="flex items-start gap-3 rounded-lg border border-slate-200 bg-white/80 p-4 shadow-sm backdrop-blur">
      <div className="mt-0.5 flex size-10 items-center justify-center rounded-md bg-cyan-50 text-cyan-700">
        <Icon className="size-5" aria-hidden="true" />
      </div>
      <div>
        <p className="text-sm font-semibold text-slate-950">{title}</p>
        <p className="mt-1 text-sm leading-6 text-slate-500">{text}</p>
      </div>
    </div>
  )
}

export default TenantRegistrationPage
