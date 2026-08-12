import { useCallback, useEffect, useState } from 'react'
import { motion } from 'framer-motion'
import { UserPlus, KeyRound, Trash2, Users as UsersIcon } from 'lucide-react'
import { toast } from 'sonner'
import { api, type UserDto } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import ConfirmDialog from '../components/ConfirmDialog'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Skeleton } from '@/components/ui/skeleton'

const PASSWORD_HINT = 'At least 6 characters, including an uppercase letter, a lowercase letter, a number and a symbol.'

export default function UsersPage() {
  const { user: me } = useAuth()
  const [users, setUsers] = useState<UserDto[]>([])
  const [loading, setLoading] = useState(true)
  const [addOpen, setAddOpen] = useState(false)
  const [resetTarget, setResetTarget] = useState<UserDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<UserDto | null>(null)

  const load = useCallback(async () => {
    try {
      setUsers(await api.users.list())
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load users')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const remove = async () => {
    if (!deleteTarget) return
    try {
      await api.users.remove(deleteTarget.id)
      toast.success('User deleted')
      await load()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to delete user')
    }
  }

  return (
    <div>
      <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="font-display text-2xl font-semibold tracking-tight">Users</h1>
          <p className="text-sm text-muted-foreground">Anyone here can sign in and use the whole console.</p>
        </div>
        <Button onClick={() => setAddOpen(true)}><UserPlus className="size-4" /> Add user</Button>
      </div>

      <div className="overflow-hidden rounded-xl border bg-card">
        <Table>
          <TableHeader>
            <TableRow className="hover:bg-transparent">
              <TableHead>Email</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && users.length === 0 &&
              Array.from({ length: 3 }).map((_, i) => (
                <TableRow key={i}><TableCell colSpan={3}><Skeleton className="h-5 w-full" /></TableCell></TableRow>
              ))}

            {!loading && users.length === 0 && (
              <TableRow className="hover:bg-transparent">
                <TableCell colSpan={3}>
                  <div className="flex flex-col items-center gap-3 py-14 text-center">
                    <div className="flex size-11 items-center justify-center rounded-full bg-muted text-muted-foreground"><UsersIcon className="size-5" /></div>
                    <div className="text-sm text-muted-foreground">No users yet.</div>
                    <Button size="sm" onClick={() => setAddOpen(true)}><UserPlus className="size-4" /> Add user</Button>
                  </div>
                </TableCell>
              </TableRow>
            )}

            {users.map((u, i) => {
              const isMe = !!me?.email && u.email?.toLowerCase() === me.email.toLowerCase()
              return (
                <motion.tr
                  key={u.id}
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  transition={{ delay: Math.min(i * 0.02, 0.2) }}
                  className="border-b transition-colors hover:bg-muted/40"
                >
                  <TableCell className="font-medium">
                    <span className="font-mono text-sm">{u.email}</span>
                    {isMe && <span className="ml-2 rounded border bg-secondary px-1.5 py-0.5 text-[10px] font-medium text-secondary-foreground">you</span>}
                  </TableCell>
                  <TableCell className="text-muted-foreground">
                    {u.lockedOut ? <span className="text-amber-800 dark:text-amber-300">Locked out</span> : 'Active'}
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex items-center justify-end gap-1">
                      <Button variant="ghost" size="sm" className="text-muted-foreground" onClick={() => setResetTarget(u)}>
                        <KeyRound className="size-4" /> Reset password
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="text-muted-foreground hover:text-destructive disabled:opacity-30"
                        onClick={() => setDeleteTarget(u)}
                        disabled={isMe}
                        title={isMe ? "You can't delete your own account" : 'Delete user'}
                      >
                        <Trash2 className="size-4" />
                      </Button>
                    </div>
                  </TableCell>
                </motion.tr>
              )
            })}
          </TableBody>
        </Table>
      </div>

      <AddUserDialog open={addOpen} onOpenChange={setAddOpen} onCreated={load} />
      <ResetPasswordDialog target={resetTarget} onOpenChange={(v) => !v && setResetTarget(null)} />
      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(v) => !v && setDeleteTarget(null)}
        title="Delete this user?"
        description={`${deleteTarget?.email} will no longer be able to sign in. This can't be undone.`}
        confirmLabel="Delete"
        destructive
        onConfirm={remove}
      />
    </div>
  )
}

function AddUserDialog({ open, onOpenChange, onCreated }: { open: boolean; onOpenChange: (v: boolean) => void; onCreated: () => void }) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (!open) {
      setEmail('')
      setPassword('')
    }
  }, [open])

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setBusy(true)
    try {
      await api.users.create(email.trim(), password)
      toast.success('User created')
      onOpenChange(false)
      onCreated()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to create user')
    } finally {
      setBusy(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Add user</DialogTitle>
          <DialogDescription>Creates a login with full access to the console.</DialogDescription>
        </DialogHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="new-email">Email</Label>
            <Input id="new-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoComplete="off" placeholder="user@example.com" />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="new-password">Password</Label>
            <Input id="new-password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required autoComplete="new-password" placeholder="Password" />
            <p className="text-xs text-muted-foreground">{PASSWORD_HINT}</p>
          </div>
          <DialogFooter>
            <Button type="button" variant="ghost" onClick={() => onOpenChange(false)}>Cancel</Button>
            <Button type="submit" disabled={busy}>{busy ? 'Creating…' : 'Create user'}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

function ResetPasswordDialog({ target, onOpenChange }: { target: UserDto | null; onOpenChange: (v: boolean) => void }) {
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    setPassword('')
  }, [target])

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!target) return
    setBusy(true)
    try {
      await api.users.resetPassword(target.id, password)
      toast.success('Password reset')
      onOpenChange(false)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to reset password')
    } finally {
      setBusy(false)
    }
  }

  return (
    <Dialog open={!!target} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Reset password</DialogTitle>
          <DialogDescription className="truncate">Set a new password for {target?.email}.</DialogDescription>
        </DialogHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="reset-password">New password</Label>
            <Input id="reset-password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required autoComplete="new-password" placeholder="Password" />
            <p className="text-xs text-muted-foreground">{PASSWORD_HINT}</p>
          </div>
          <DialogFooter>
            <Button type="button" variant="ghost" onClick={() => onOpenChange(false)}>Cancel</Button>
            <Button type="submit" disabled={busy}>{busy ? 'Saving…' : 'Reset password'}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
