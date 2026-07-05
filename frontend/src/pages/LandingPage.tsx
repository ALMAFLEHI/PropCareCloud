import {
  ArrowRight,
  Building2,
  ClipboardList,
  Clock3,
  LockKeyhole,
  MessageSquare,
  ShieldCheck,
  UserCheck,
  Wrench,
} from 'lucide-react'
import { Link } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

const features = [
  {
    title: 'Tenant service requests',
    description:
      'Residents can report maintenance issues and follow progress from one clear workspace.',
    icon: ClipboardList,
  },
  {
    title: 'Manager assignment workflow',
    description:
      'Property teams can review priorities, assign staff, and coordinate work smoothly.',
    icon: UserCheck,
  },
  {
    title: 'Staff progress tracking',
    description:
      'Maintenance teams can view assigned work, update progress, and add service notes.',
    icon: Wrench,
  },
  {
    title: 'Secure role-based access',
    description:
      'Each account sees the right portfolio, request, and access management areas.',
    icon: LockKeyhole,
  },
]

const operations = [
  'Request tracking',
  'Unit-level maintenance visibility',
  'Staff assignment',
  'Activity notes',
  'Progress timeline',
]

function LandingPage() {
  const { isAuthenticated } = useAuth()

  return (
    <main className="premium-public-shell min-h-screen text-slate-950">
      <header className="mx-auto flex w-full max-w-7xl items-center justify-between px-4 py-6 sm:px-6 lg:px-8">
        <Link to="/welcome" className="flex items-center gap-3">
          <div className="flex size-11 items-center justify-center rounded-xl bg-cyan-700 text-white shadow-sm shadow-cyan-900/20">
            <Building2 className="size-5" aria-hidden="true" />
          </div>
          <div>
            <p className="text-base font-semibold text-slate-950">
              PropCare Cloud
            </p>
            <p className="text-xs font-medium text-slate-500">
              Property Maintenance Portal
            </p>
          </div>
        </Link>

        <div className="flex items-center gap-2">
          {isAuthenticated && (
            <Link
              to="/"
              className="hidden rounded-md border border-slate-200 bg-white/80 px-3 py-2 text-sm font-semibold text-slate-700 shadow-sm transition hover:border-cyan-200 hover:bg-white sm:inline-flex"
            >
              Dashboard
            </Link>
          )}
          <Link
            to="/login"
            className="inline-flex items-center gap-2 rounded-md bg-slate-950 px-4 py-2 text-sm font-semibold text-white shadow-sm shadow-slate-900/20 transition hover:bg-cyan-800"
          >
            Sign in
            <ArrowRight className="size-4" aria-hidden="true" />
          </Link>
        </div>
      </header>

      <section className="mx-auto grid w-full max-w-7xl items-center gap-10 px-4 pb-16 pt-8 sm:px-6 lg:grid-cols-[1fr_0.92fr] lg:px-8 lg:pb-20 lg:pt-14">
        <div className="max-w-3xl">
          <p className="premium-eyebrow">PropCare Cloud</p>
          <h1 className="mt-5 text-4xl font-semibold leading-tight tracking-normal text-slate-950 sm:text-5xl lg:text-6xl">
            Property maintenance, requests, and service progress in one secure
            portal.
          </h1>
          <p className="mt-6 max-w-2xl text-base leading-8 text-slate-600 sm:text-lg">
            PropCare Cloud helps tenants report issues, managers coordinate
            service work, and maintenance teams track progress from one
            connected workspace.
          </p>

          <div className="mt-8 flex flex-col gap-3 sm:flex-row">
            <Link
              to="/login"
              className="inline-flex items-center justify-center gap-2 rounded-md bg-cyan-700 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-cyan-900/20 transition hover:-translate-y-0.5 hover:bg-cyan-800 hover:shadow-cyan-900/25"
            >
              Sign in to portal
              <ArrowRight className="size-4" aria-hidden="true" />
            </Link>
            <a
              href="#features"
              className="inline-flex items-center justify-center rounded-md border border-slate-200 bg-white/80 px-5 py-3 text-sm font-semibold text-slate-700 shadow-sm transition hover:-translate-y-0.5 hover:border-cyan-200 hover:bg-white"
            >
              View features
            </a>
            {isAuthenticated && (
              <Link
                to="/"
                className="inline-flex items-center justify-center rounded-md border border-emerald-200 bg-emerald-50 px-5 py-3 text-sm font-semibold text-emerald-800 transition hover:-translate-y-0.5 hover:bg-emerald-100"
              >
                Go to dashboard
              </Link>
            )}
          </div>
        </div>

        <div className="premium-panel p-4 sm:p-5">
          <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <div className="flex items-center justify-between gap-3 border-b border-slate-100 pb-4">
              <div>
                <p className="text-sm font-semibold text-slate-950">
                  Service Operations
                </p>
                <p className="mt-1 text-xs text-slate-500">
                  Live request coordination preview
                </p>
              </div>
              <span className="rounded-md bg-emerald-50 px-2.5 py-1 text-xs font-semibold text-emerald-800 ring-1 ring-inset ring-emerald-200">
                Connected
              </span>
            </div>

            <div className="mt-4 grid gap-3 sm:grid-cols-2">
              <PreviewMetric label="Open requests" value="18" />
              <PreviewMetric label="Assigned jobs" value="12" />
              <PreviewMetric label="Urgent priority" value="4" />
              <PreviewMetric label="Resolved this week" value="27" />
            </div>

            <div className="mt-5 rounded-lg border border-slate-200 bg-slate-50 p-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <p className="text-sm font-semibold text-slate-950">
                    Water leak in kitchen
                  </p>
                  <p className="mt-1 text-xs text-slate-500">
                    Cloud Residence - Unit A-0205
                  </p>
                </div>
                <span className="rounded-md bg-cyan-50 px-2.5 py-1 text-xs font-semibold text-cyan-800 ring-1 ring-inset ring-cyan-200">
                  In Progress
                </span>
              </div>

              <div className="mt-5 grid gap-2 sm:grid-cols-5">
                {['Submitted', 'Review', 'Assigned', 'Work', 'Done'].map(
                  (step, index) => (
                    <div
                      key={step}
                      className={`rounded-md border px-2 py-2 text-center text-xs font-semibold ${
                        index < 4
                          ? 'border-cyan-200 bg-cyan-50 text-cyan-800'
                          : 'border-slate-200 bg-white text-slate-400'
                      }`}
                    >
                      {step}
                    </div>
                  ),
                )}
              </div>
            </div>

            <div className="mt-4 grid gap-3">
              <PreviewActivity
                icon={MessageSquare}
                title="Activity note added"
                text="Maintenance team confirmed arrival window."
              />
              <PreviewActivity
                icon={Clock3}
                title="Progress updated"
                text="Assigned work moved into progress tracking."
              />
            </div>
          </div>
        </div>
      </section>

      <section
        id="features"
        className="mx-auto w-full max-w-7xl px-4 pb-14 sm:px-6 lg:px-8"
      >
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {features.map((feature) => {
            const Icon = feature.icon

            return (
              <article key={feature.title} className="premium-card p-5">
                <div className="flex size-11 items-center justify-center rounded-lg bg-cyan-50 text-cyan-700">
                  <Icon className="size-5" aria-hidden="true" />
                </div>
                <h2 className="mt-4 text-base font-semibold text-slate-950">
                  {feature.title}
                </h2>
                <p className="mt-3 text-sm leading-6 text-slate-600">
                  {feature.description}
                </p>
              </article>
            )
          })}
        </div>
      </section>

      <section className="mx-auto w-full max-w-7xl px-4 pb-16 sm:px-6 lg:px-8">
        <div className="premium-panel grid gap-6 p-6 lg:grid-cols-[0.72fr_1fr] lg:p-8">
          <div>
            <p className="premium-eyebrow">Built for property operations</p>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">
              A connected workspace for maintenance visibility.
            </h2>
            <p className="mt-4 text-sm leading-7 text-slate-600">
              Keep service activity organized from the first resident report to
              assignment, notes, progress tracking, and completion.
            </p>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            {operations.map((item) => (
              <div
                key={item}
                className="flex items-center gap-3 rounded-lg border border-slate-200 bg-white/80 p-4 text-sm font-semibold text-slate-700 shadow-sm"
              >
                <ShieldCheck className="size-4 text-cyan-700" aria-hidden="true" />
                {item}
              </div>
            ))}
          </div>
        </div>
      </section>

      <footer className="border-t border-white/70 bg-white/60 px-4 py-6 backdrop-blur sm:px-6 lg:px-8">
        <div className="mx-auto flex w-full max-w-7xl flex-col gap-2 text-sm text-slate-500 sm:flex-row sm:items-center sm:justify-between">
          <p className="font-semibold text-slate-700">PropCare Cloud</p>
          <p>Property Maintenance Portal</p>
        </div>
      </footer>
    </main>
  )
}

function PreviewMetric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-3">
      <p className="text-xs font-medium text-slate-500">{label}</p>
      <p className="mt-2 text-2xl font-semibold text-slate-950">{value}</p>
    </div>
  )
}

function PreviewActivity({
  icon: Icon,
  title,
  text,
}: {
  icon: typeof MessageSquare
  title: string
  text: string
}) {
  return (
    <div className="flex items-start gap-3 rounded-lg border border-slate-200 bg-white p-3">
      <div className="mt-0.5 flex size-8 items-center justify-center rounded-md bg-slate-100 text-slate-600">
        <Icon className="size-4" aria-hidden="true" />
      </div>
      <div>
        <p className="text-sm font-semibold text-slate-950">{title}</p>
        <p className="mt-1 text-xs leading-5 text-slate-500">{text}</p>
      </div>
    </div>
  )
}

export default LandingPage
