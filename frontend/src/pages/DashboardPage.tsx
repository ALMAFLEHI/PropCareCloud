import {
  Building2,
  ClipboardList,
  Hammer,
  Home,
  ShieldCheck,
  UserRound,
  UsersRound,
  Wrench,
} from 'lucide-react'
import ApiStatusCard from '../components/ApiStatusCard'
import RoleCard from '../components/RoleCard'
import StatCard from '../components/StatCard'

const roleCards = [
  {
    title: 'Admin / Owner',
    description: 'Oversees the platform, property portfolio, and cloud progress.',
    responsibilities: ['Manage ownership view', 'Review reports', 'Track service quality'],
    icon: ShieldCheck,
  },
  {
    title: 'Property Manager',
    description: 'Coordinates tenant requests and maintenance work assignments.',
    responsibilities: ['Review requests', 'Assign staff', 'Monitor outcomes'],
    icon: ClipboardList,
  },
  {
    title: 'Tenant',
    description: 'Submits maintenance issues and follows request progress.',
    responsibilities: ['Create requests', 'Provide details', 'View updates'],
    icon: UserRound,
  },
  {
    title: 'Maintenance Staff',
    description: 'Receives assigned work and records repair progress.',
    responsibilities: ['Accept jobs', 'Update status', 'Record completion notes'],
    icon: Hammer,
  },
]

function DashboardPage() {
  return (
    <div className="space-y-6">
      <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
        <div className="max-w-4xl">
          <p className="text-sm font-semibold text-cyan-700">
            CT071-3-3-DDAC Cloud Application
          </p>
          <h2 className="mt-3 text-3xl font-semibold text-slate-950 sm:text-4xl">
            Property maintenance and tenant service portal
          </h2>
          <p className="mt-4 text-base leading-7 text-slate-600">
            PropCare Cloud is a cloud-ready dashboard foundation for handling
            tenant maintenance requests, property operations, role-based work,
            and future AWS service integration.
          </p>
        </div>
      </section>

      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          title="Open Requests"
          value="18"
          helperText="Placeholder count for future maintenance workflow data."
          icon={Wrench}
        />
        <StatCard
          title="Properties"
          value="6"
          helperText="Represents managed buildings and rental units."
          icon={Building2}
        />
        <StatCard
          title="Active Tenants"
          value="42"
          helperText="Future tenant records will come from the database."
          icon={UsersRound}
        />
        <StatCard
          title="Staff Members"
          value="9"
          helperText="Maintenance staff accounts will be managed later."
          icon={Hammer}
        />
      </section>

      <ApiStatusCard />

      <section>
        <div className="mb-4 flex items-center gap-2">
          <UsersRound className="size-5 text-cyan-700" aria-hidden="true" />
          <h2 className="text-xl font-semibold text-slate-950">
            Planned User Roles
          </h2>
        </div>
        <div className="grid gap-4 lg:grid-cols-2 xl:grid-cols-4">
          {roleCards.map((role) => (
            <RoleCard key={role.title} {...role} />
          ))}
        </div>
      </section>

      <section className="grid gap-4 lg:grid-cols-2">
        <article className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <div className="flex items-center gap-2">
            <Home className="size-5 text-cyan-700" aria-hidden="true" />
            <h2 className="text-lg font-semibold text-slate-950">
              Cloud Architecture Summary
            </h2>
          </div>
          <p className="mt-4 rounded-md bg-slate-50 p-4 text-sm font-medium text-slate-700">
            React Frontend -&gt; ASP.NET Core Web API -&gt; Amazon RDS PostgreSQL
          </p>
        </article>

        <article className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <div className="flex items-center gap-2">
            <Building2 className="size-5 text-indigo-700" aria-hidden="true" />
            <h2 className="text-lg font-semibold text-slate-950">
              Future AWS Services
            </h2>
          </div>
          <p className="mt-4 text-sm leading-6 text-slate-600">
            Planned cloud extensions include S3, API Gateway, Lambda, SNS/SQS,
            and CloudWatch/X-Ray for storage, integration, messaging, and
            observability.
          </p>
        </article>
      </section>
    </div>
  )
}

export default DashboardPage
