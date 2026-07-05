import { Building2, LayoutDashboard, UsersRound, Wrench } from 'lucide-react'
import { NavLink } from 'react-router-dom'
import type { LucideIcon } from 'lucide-react'
import { useAuth } from '../context/AuthContext'
import type { UserRoleKey } from '../utils/roles'

type NavigationItem = {
  label: string
  to: string
  icon: LucideIcon
  roles: UserRoleKey[]
}

const navigationItems: NavigationItem[] = [
  {
    label: 'Dashboard',
    to: '/',
    icon: LayoutDashboard,
    roles: ['AdminOwner', 'PropertyManager', 'Tenant', 'MaintenanceStaff'],
  },
  {
    label: 'Maintenance Requests',
    to: '/requests',
    icon: Wrench,
    roles: ['AdminOwner', 'PropertyManager', 'Tenant', 'MaintenanceStaff'],
  },
  {
    label: 'Properties',
    to: '/properties',
    icon: Building2,
    roles: ['AdminOwner', 'PropertyManager'],
  },
  {
    label: 'Access Management',
    to: '/users',
    icon: UsersRound,
    roles: ['AdminOwner'],
  },
]

function Sidebar() {
  const { userRoleKey } = useAuth()
  const visibleItems = navigationItems.filter(
    (item) => userRoleKey && item.roles.includes(userRoleKey),
  )

  return (
    <aside className="border-b border-slate-200/80 bg-white/90 shadow-sm backdrop-blur md:min-h-screen md:w-72 md:border-b-0 md:border-r">
      <div className="flex h-full flex-col gap-5 px-4 py-4 sm:px-6 md:px-5 md:py-6">
        <div className="flex items-center gap-3">
          <div className="flex size-10 items-center justify-center rounded-lg bg-cyan-700 text-white shadow-sm">
            <Building2 className="size-5" aria-hidden="true" />
          </div>
          <div>
            <p className="text-sm font-semibold text-slate-950">PropCare</p>
            <p className="text-xs text-slate-500">Cloud Portal</p>
          </div>
        </div>

        <nav className="flex gap-2 overflow-x-auto pb-1 md:flex-col md:overflow-visible md:pb-0">
          {visibleItems.map((item) => {
            const Icon = item.icon

            return (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === '/'}
                className={({ isActive }) =>
                  [
                    'flex min-w-max items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition',
                    isActive
                      ? 'bg-cyan-700 text-white shadow-sm shadow-cyan-900/20'
                      : 'text-slate-600 hover:bg-cyan-50 hover:text-slate-950',
                  ].join(' ')
                }
              >
                <Icon className="size-4 shrink-0" aria-hidden="true" />
                <span>{item.label}</span>
              </NavLink>
            )
          })}
        </nav>
      </div>
    </aside>
  )
}

export default Sidebar
