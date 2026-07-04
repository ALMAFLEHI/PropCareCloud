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
          Authentication and user management remain intentionally deferred. Sprint
          8 uses seeded local user profiles only for CRUD validation, while this
          page records the future role-based access model.
        </p>
      </section>

      <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
        <h3 className="text-base font-semibold text-slate-950">
          Sprint 8 scope note
        </h3>
        <p className="mt-3 max-w-4xl text-sm leading-6 text-slate-600">
          No login screen, password handling, JWT flow, or authorization
          enforcement has been added yet. Those security features will be handled
          in a later sprint after the core CRUD screens are validated.
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
