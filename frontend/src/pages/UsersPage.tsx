import { Hammer, ShieldCheck, UserRound, UsersRound } from 'lucide-react'
import RoleCard from '../components/RoleCard'

const accessRoles = [
  {
    title: 'Admin / Owner',
    description: 'Owns system oversight, properties, users, and reporting direction.',
    responsibilities: ['Manage users', 'Review portfolio', 'Oversee compliance'],
    icon: ShieldCheck,
  },
  {
    title: 'Property Manager',
    description: 'Runs day-to-day request triage and operational assignment.',
    responsibilities: ['Assign requests', 'Update priorities', 'Coordinate repairs'],
    icon: UsersRound,
  },
  {
    title: 'Tenant',
    description: 'Uses the portal to report issues and receive progress updates.',
    responsibilities: ['Submit issues', 'View status', 'Confirm completion'],
    icon: UserRound,
  },
  {
    title: 'Maintenance Staff',
    description: 'Completes assigned work and documents maintenance progress.',
    responsibilities: ['Receive jobs', 'Update work', 'Close tasks'],
    icon: Hammer,
  },
]

function UsersPage() {
  return (
    <div className="space-y-6">
      <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
        <p className="text-sm font-semibold text-cyan-700">Users / Roles</p>
        <h2 className="mt-3 text-3xl font-semibold text-slate-950">
          Role-based access foundation
        </h2>
        <p className="mt-4 max-w-3xl text-base leading-7 text-slate-600">
          Sprint 9 adds demo authentication and role-based navigation. This
          admin-only page records the access model that will later become full
          production user management.
        </p>
      </section>

      <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
        <h3 className="text-base font-semibold text-slate-950">
          Sprint 9 scope note
        </h3>
        <p className="mt-3 max-w-4xl text-sm leading-6 text-slate-600">
          Demo JWT login is active for assignment testing. Production password
          reset, email invitations, and cloud identity services remain deferred.
        </p>
      </section>

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {accessRoles.map((role) => (
          <RoleCard key={role.title} {...role} />
        ))}
      </section>
    </div>
  )
}

export default UsersPage
