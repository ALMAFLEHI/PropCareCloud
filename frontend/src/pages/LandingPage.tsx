import {
  ArrowRight,
  Building2,
  CheckCircle2,
  ClipboardList,
  Clock3,
  DoorOpen,
  FileText,
  History,
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
      'Residents can report issues with unit, category, and priority details.',
    icon: ClipboardList,
  },
  {
    title: 'Manager assignment workflow',
    description:
      'Property teams can review incoming work and coordinate the right staff member.',
    icon: UserCheck,
  },
  {
    title: 'Staff progress tracking',
    description:
      'Maintenance teams can update progress and keep activity notes in one place.',
    icon: Wrench,
  },
  {
    title: 'Secure access control',
    description:
      'Operational views stay focused on the areas each account should use.',
    icon: LockKeyhole,
  },
]

const workflowSteps = [
  {
    title: 'Report issue',
    description:
      'Tenants submit maintenance requests with unit and category details.',
    icon: FileText,
  },
  {
    title: 'Assign maintenance',
    description:
      'Managers review requests and coordinate the right staff member.',
    icon: UserCheck,
  },
  {
    title: 'Track completion',
    description:
      'Teams update progress while tenants follow status and activity notes.',
    icon: CheckCircle2,
  },
]

const trustCues = [
  'Secure role-based access',
  'Tenant privacy',
  'Manager oversight',
  'Staff assignment tracking',
  'Activity history',
]

function LandingPage() {
  const { isAuthenticated } = useAuth()

  return (
    <main className="premium-public-shell blueprint-surface min-h-screen overflow-hidden text-slate-950">
      <header className="relative z-10 mx-auto flex w-full max-w-7xl items-center justify-between px-4 py-6 sm:px-6 lg:px-8">
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
            to="/register"
            className="hidden rounded-md border border-cyan-200 bg-white/80 px-3 py-2 text-sm font-semibold text-cyan-800 shadow-sm transition hover:border-cyan-300 hover:bg-cyan-50 sm:inline-flex"
          >
            Request tenant access
          </Link>
          <Link
            to="/login"
            className="inline-flex items-center gap-2 rounded-md bg-slate-950 px-4 py-2 text-sm font-semibold text-white shadow-sm shadow-slate-900/20 transition hover:-translate-y-0.5 hover:bg-cyan-800"
          >
            Sign in
            <ArrowRight className="size-4" aria-hidden="true" />
          </Link>
        </div>
      </header>

      <section className="relative z-10 mx-auto grid w-full max-w-7xl items-center gap-10 px-4 pb-16 pt-8 sm:px-6 lg:grid-cols-[0.9fr_1.1fr] lg:px-8 lg:pb-20 lg:pt-14">
        <div className="max-w-3xl">
          <p className="premium-eyebrow">Property operations, connected</p>
          <h1 className="mt-5 text-4xl font-semibold leading-tight tracking-normal text-slate-950 sm:text-5xl lg:text-6xl">
            A smarter way to manage property maintenance.
          </h1>
          <p className="mt-6 max-w-2xl text-base leading-8 text-slate-600 sm:text-lg">
            PropCare Cloud connects tenants, property managers, and maintenance
            teams through one secure service portal for requests, assignments,
            progress tracking, and activity history.
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
              href="#how-it-works"
              className="inline-flex items-center justify-center rounded-md border border-slate-200 bg-white/80 px-5 py-3 text-sm font-semibold text-slate-700 shadow-sm transition hover:-translate-y-0.5 hover:border-cyan-200 hover:bg-white"
            >
              See how it works
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

          <div className="mt-5 max-w-2xl rounded-lg border border-cyan-100 bg-white/75 p-4 shadow-sm backdrop-blur">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex items-start gap-3">
                <div className="mt-0.5 flex size-10 shrink-0 items-center justify-center rounded-md bg-cyan-50 text-cyan-700">
                  <UserCheck className="size-5" aria-hidden="true" />
                </div>
                <div>
                  <p className="text-sm font-semibold text-slate-950">
                    Need tenant access?
                  </p>
                  <p className="mt-1 text-sm leading-6 text-slate-600">
                    Submit a request and your property manager can approve your portal account.
                  </p>
                </div>
              </div>
              <Link
                to="/register"
                className="inline-flex shrink-0 items-center justify-center rounded-md border border-cyan-200 bg-cyan-50 px-3 py-2 text-sm font-semibold text-cyan-800 shadow-sm transition hover:-translate-y-0.5 hover:border-cyan-300 hover:bg-white"
              >
                Request tenant access
              </Link>
            </div>
          </div>

          <div className="mt-8 grid gap-3 sm:grid-cols-3">
            <HeroSignal label="Requests" value="Tracked" />
            <HeroSignal label="Assignments" value="Coordinated" />
            <HeroSignal label="Progress" value="Visible" />
          </div>
        </div>

        <ProductMockup />
      </section>

      <section className="relative z-10 mx-auto w-full max-w-7xl px-4 pb-14 sm:px-6 lg:px-8">
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

      <section
        id="how-it-works"
        className="relative z-10 mx-auto w-full max-w-7xl px-4 pb-14 sm:px-6 lg:px-8"
      >
        <div className="mb-6 max-w-2xl">
          <p className="premium-eyebrow">How it works</p>
          <h2 className="mt-4 text-3xl font-semibold text-slate-950">
            From issue report to completion, every step stays visible.
          </h2>
        </div>
        <div className="grid gap-4 lg:grid-cols-3">
          {workflowSteps.map((step, index) => {
            const Icon = step.icon

            return (
              <article key={step.title} className="premium-card p-5">
                <div className="flex items-center justify-between gap-3">
                  <div className="flex size-11 items-center justify-center rounded-lg bg-slate-950 text-white">
                    <Icon className="size-5" aria-hidden="true" />
                  </div>
                  <span className="text-sm font-semibold text-cyan-700">
                    0{index + 1}
                  </span>
                </div>
                <h3 className="mt-5 text-lg font-semibold text-slate-950">
                  {step.title}
                </h3>
                <p className="mt-3 text-sm leading-6 text-slate-600">
                  {step.description}
                </p>
              </article>
            )
          })}
        </div>
      </section>

      <section className="relative z-10 mx-auto w-full max-w-7xl px-4 pb-16 sm:px-6 lg:px-8">
        <div className="premium-panel grid gap-6 p-6 lg:grid-cols-[0.7fr_1fr] lg:p-8">
          <div>
            <p className="premium-eyebrow">Built for secure property operations</p>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">
              Professional access, privacy, and operational control.
            </h2>
            <p className="mt-4 text-sm leading-7 text-slate-600">
              PropCare Cloud keeps service activity organized while separating
              portfolio, tenant, and maintenance team responsibilities.
            </p>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            {trustCues.map((item) => (
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

      <footer className="relative z-10 border-t border-white/70 bg-white/60 px-4 py-6 backdrop-blur sm:px-6 lg:px-8">
        <div className="mx-auto flex w-full max-w-7xl flex-col gap-2 text-sm text-slate-500 sm:flex-row sm:items-center sm:justify-between">
          <p className="font-semibold text-slate-700">PropCare Cloud</p>
          <p>Property Maintenance Portal</p>
          <p>Built for property operations</p>
        </div>
      </footer>
    </main>
  )
}

function HeroSignal({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-white/80 bg-white/70 p-3 shadow-sm backdrop-blur">
      <p className="text-xs font-medium text-slate-500">{label}</p>
      <p className="mt-1 text-sm font-semibold text-slate-950">{value}</p>
    </div>
  )
}

function ProductMockup() {
  return (
    <div className="mockup-shell">
      <div className="mockup-browser">
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
          <div className="flex items-center gap-2">
            <span className="size-2.5 rounded-full bg-rose-300" />
            <span className="size-2.5 rounded-full bg-amber-300" />
            <span className="size-2.5 rounded-full bg-emerald-300" />
          </div>
          <div className="hidden rounded-md bg-slate-100 px-16 py-1 text-xs text-slate-400 sm:block">
            propcare.cloud/portal
          </div>
          <span className="rounded-md bg-emerald-50 px-2 py-1 text-xs font-semibold text-emerald-800">
            Secure
          </span>
        </div>

        <div className="grid min-h-[430px] grid-cols-[86px_1fr] bg-white">
          <aside className="border-r border-slate-200 bg-slate-950 p-3 text-white">
            <div className="mb-6 flex size-9 items-center justify-center rounded-lg bg-cyan-600">
              <Building2 className="size-4" aria-hidden="true" />
            </div>
            <div className="space-y-2">
              {['Home', 'Requests', 'Units', 'Access'].map((item, index) => (
                <div
                  key={item}
                  className={`rounded-md px-2 py-2 text-[11px] font-semibold ${
                    index === 1 ? 'bg-cyan-600 text-white' : 'text-slate-400'
                  }`}
                >
                  {item}
                </div>
              ))}
            </div>
          </aside>

          <section className="bg-slate-50 p-4">
            <div className="mb-4 flex flex-wrap items-center justify-between gap-3 rounded-lg border border-slate-200 bg-white p-3">
              <div>
                <p className="text-xs font-semibold text-cyan-700">
                  Maintenance Request
                </p>
                <p className="mt-1 text-sm font-semibold text-slate-950">
                  Kitchen leak repair
                </p>
              </div>
              <span className="rounded-md bg-cyan-50 px-2.5 py-1 text-xs font-semibold text-cyan-800 ring-1 ring-inset ring-cyan-200">
                In Progress
              </span>
            </div>

            <div className="grid gap-3 lg:grid-cols-[1fr_160px]">
              <div className="space-y-3">
                <div className="rounded-lg border border-slate-200 bg-white p-4">
                  <div className="flex items-center gap-2">
                    <DoorOpen className="size-4 text-cyan-700" aria-hidden="true" />
                    <p className="text-sm font-semibold text-slate-950">
                      Cloud Residence - Unit A-0205
                    </p>
                  </div>
                  <p className="mt-3 text-xs leading-5 text-slate-500">
                    Water is leaking under the kitchen sink and needs inspection.
                  </p>
                  <div className="mt-4 grid gap-1.5 sm:grid-cols-5">
                    {['Submitted', 'Review', 'Assigned', 'Work', 'Done'].map(
                      (step, index) => (
                        <div
                          key={step}
                          className={`min-w-0 rounded-md border px-1 py-2 text-center text-[9px] font-semibold leading-none ${
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

                <div className="rounded-lg border border-slate-200 bg-white p-4">
                  <div className="flex items-center gap-2">
                    <History className="size-4 text-cyan-700" aria-hidden="true" />
                    <p className="text-sm font-semibold text-slate-950">
                      Activity history
                    </p>
                  </div>
                  <div className="mt-3 space-y-3">
                    <MockupNote
                      icon={MessageSquare}
                      title="Staff note added"
                      text="Inspection scheduled and tenant notified."
                    />
                    <MockupNote
                      icon={Clock3}
                      title="Progress updated"
                      text="Request moved to in progress."
                    />
                  </div>
                </div>
              </div>

              <div className="space-y-3">
                <MockupMetric label="Priority" value="High" />
                <MockupMetric label="Assigned to" value="Nadia Staff" />
                <MockupMetric label="Notes" value="4" />
              </div>
            </div>
          </section>
        </div>
      </div>
    </div>
  )
}

function MockupMetric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-3 shadow-sm">
      <p className="text-xs font-medium text-slate-500">{label}</p>
      <p className="mt-2 text-sm font-semibold text-slate-950">{value}</p>
    </div>
  )
}

function MockupNote({
  icon: Icon,
  title,
  text,
}: {
  icon: typeof MessageSquare
  title: string
  text: string
}) {
  return (
    <div className="flex items-start gap-3 rounded-md bg-slate-50 p-3">
      <div className="mt-0.5 flex size-7 items-center justify-center rounded-md bg-white text-cyan-700 shadow-sm">
        <Icon className="size-3.5" aria-hidden="true" />
      </div>
      <div>
        <p className="text-xs font-semibold text-slate-950">{title}</p>
        <p className="mt-1 text-xs leading-5 text-slate-500">{text}</p>
      </div>
    </div>
  )
}

export default LandingPage
