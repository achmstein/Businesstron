import { useEffect, useState } from 'react'
import { Eye, EyeOff } from 'lucide-react'
import { toast } from 'sonner'
import { api, type AsicConnectionTestResult, type AsicSettingsInput, type TwoCaptchaCredentials } from '../api/client'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import { cn } from '@/lib/utils'

function StatusPill({
  configured,
  onLabel = 'Connected',
  offLabel = 'Not configured',
  title,
}: {
  configured: boolean
  onLabel?: string
  offLabel?: string
  title?: string
}) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 text-xs font-medium',
        configured ? 'text-emerald-700 dark:text-emerald-400' : 'text-muted-foreground',
      )}
      title={title ?? (configured ? 'Credentials are set' : 'Credentials are missing')}
    >
      <span className="relative flex size-2">
        {configured && <span className="absolute inline-flex size-full animate-ping rounded-full bg-emerald-400 opacity-75" />}
        <span className={cn('relative inline-flex size-2 rounded-full', configured ? 'bg-emerald-500' : 'bg-muted-foreground/50')} />
      </span>
      {configured ? onLabel : offLabel}
    </span>
  )
}

function SecretInput({
  id,
  value,
  onChange,
  placeholder,
}: {
  id: string
  value: string
  onChange: (v: string) => void
  placeholder?: string
}) {
  const [show, setShow] = useState(false)
  return (
    <div className="relative">
      <Input
        id={id}
        type={show ? 'text' : 'password'}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="pr-10 font-mono"
        placeholder={placeholder}
        autoComplete="off"
        spellCheck={false}
      />
      <button
        type="button"
        tabIndex={-1}
        aria-label={show ? 'Hide value' : 'Show value'}
        onClick={() => setShow((s) => !s)}
        className="absolute inset-y-0 right-0 flex items-center px-3 text-muted-foreground transition-colors hover:text-foreground"
      >
        {show ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
      </button>
    </div>
  )
}

export default function SettingsPage() {
  // Ontraport: API credentials + push mapping.
  const [appId, setAppId] = useState('')
  const [apiKey, setApiKey] = useState('')
  const [tagId, setTagId] = useState('')
  const [sequenceId, setSequenceId] = useState('')
  const [autoPush, setAutoPush] = useState(false)
  const [ontraportBusy, setOntraportBusy] = useState(false)

  // 2Captcha: API key (timeouts are round-tripped but not surfaced here).
  const [captcha, setCaptcha] = useState<TwoCaptchaCredentials>({
    apiKey: '',
    defaultTimeoutSeconds: 120,
    recaptchaTimeoutSeconds: 600,
    pollingIntervalSeconds: 10,
  })
  const [captchaBusy, setCaptchaBusy] = useState(false)

  // WhoisXML: reverse-WHOIS API key for the web-enrichment stage.
  const [whoisXmlKey, setWhoisXmlKey] = useState('')
  const [whoisXmlBusy, setWhoisXmlBusy] = useState(false)

  // ASIC enrichment: parallel session count, hard-capped by the server.
  const [maxConcurrency, setMaxConcurrency] = useState('')
  const [concurrencyLimit, setConcurrencyLimit] = useState(16)
  const [asicBusy, setAsicBusy] = useState(false)

  // ASIC transport: TLS 1.3 keeps Cloudflare from fingerprinting us as a bot.
  const [forceTls13, setForceTls13] = useState(true)

  // ASIC proxy: routes scraping off the server's own IP.
  const [proxyUrl, setProxyUrl] = useState('')
  const [proxyUsername, setProxyUsername] = useState('')
  const [proxyPassword, setProxyPassword] = useState('')
  const [testBusy, setTestBusy] = useState(false)
  const [testResult, setTestResult] = useState<AsicConnectionTestResult | null>(null)

  useEffect(() => {
    void (async () => {
      try {
        const [creds, cfg, cap, asic, whois] = await Promise.all([
          api.settings.getOntraportCredentials(),
          api.settings.getOntraport(),
          api.settings.getCaptcha(),
          api.settings.getAsic(),
          api.settings.getWhoisXml(),
        ])
        setAppId(creds.appId ?? '')
        setApiKey(creds.apiKey ?? '')
        setTagId(cfg.tagId?.toString() ?? '')
        setSequenceId(cfg.sequenceId?.toString() ?? '')
        setAutoPush(cfg.autoPushEnabled)
        setCaptcha({ ...cap, apiKey: cap.apiKey ?? '' })
        setWhoisXmlKey(whois.apiKey ?? '')
        setMaxConcurrency(asic.maxConcurrency.toString())
        setConcurrencyLimit(asic.maxConcurrencyLimit)
        setForceTls13(asic.forceTls13)
        setProxyUrl(asic.proxyUrl ?? '')
        setProxyUsername(asic.proxyUsername ?? '')
        setProxyPassword(asic.proxyPassword ?? '')
      } catch (err) {
        toast.error(err instanceof Error ? err.message : 'Failed to load settings')
      }
    })()
  }, [])

  const ontraportConfigured = appId.trim().length > 0 && apiKey.trim().length > 0
  const captchaConfigured = (captcha.apiKey ?? '').trim().length > 0
  const whoisXmlConfigured = whoisXmlKey.trim().length > 0

  const saveOntraport = async (e: React.FormEvent) => {
    e.preventDefault()
    setOntraportBusy(true)
    try {
      await api.settings.updateOntraportCredentials({
        appId: appId.trim() || null,
        apiKey: apiKey.trim() || null,
      })
      await api.settings.updateOntraport({
        tagId: tagId ? Number(tagId) : null,
        sequenceId: sequenceId ? Number(sequenceId) : null,
        autoPushEnabled: autoPush,
      })
      toast.success('Ontraport settings saved')
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to save')
    } finally {
      setOntraportBusy(false)
    }
  }

  // Built from the live form so Test probes exactly what Save would persist.
  const asicInput = (): AsicSettingsInput | null => {
    const value = Number(maxConcurrency)
    if (!Number.isInteger(value) || value < 1 || value > concurrencyLimit) {
      toast.error(`Parallel sessions must be between 1 and ${concurrencyLimit}`)
      return null
    }
    return {
      maxConcurrency: value,
      forceTls13,
      proxyUrl: proxyUrl.trim() || null,
      proxyUsername: proxyUsername.trim() || null,
      proxyPassword: proxyPassword || null,
    }
  }

  const saveAsic = async (e: React.FormEvent) => {
    e.preventDefault()
    const input = asicInput()
    if (!input) return

    setAsicBusy(true)
    try {
      await api.settings.updateAsic(input)
      toast.success('ASIC settings saved', { description: 'Applies from the next search run.' })
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to save')
    } finally {
      setAsicBusy(false)
    }
  }

  const testAsic = async () => {
    const input = asicInput()
    if (!input) return

    setTestBusy(true)
    setTestResult(null)
    try {
      setTestResult(await api.settings.testAsic(input))
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Connection test failed')
    } finally {
      setTestBusy(false)
    }
  }

  const saveCaptcha = async (e: React.FormEvent) => {
    e.preventDefault()
    setCaptchaBusy(true)
    try {
      await api.settings.updateCaptcha({ ...captcha, apiKey: (captcha.apiKey ?? '').trim() || null })
      toast.success('2Captcha settings saved')
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to save')
    } finally {
      setCaptchaBusy(false)
    }
  }

  const saveWhoisXml = async (e: React.FormEvent) => {
    e.preventDefault()
    setWhoisXmlBusy(true)
    try {
      await api.settings.updateWhoisXml({ apiKey: whoisXmlKey.trim() || null })
      toast.success('Reverse WHOIS settings saved')
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to save')
    } finally {
      setWhoisXmlBusy(false)
    }
  }

  return (
    <div className="mx-auto max-w-2xl space-y-8">
      <div>
        <h1 className="font-display text-2xl font-semibold tracking-tight">Settings</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Integration credentials are stored on the server and take effect immediately — no restart required.
        </p>
      </div>

      {/* Ontraport */}
      <form onSubmit={saveOntraport} className="space-y-4">
        <div>
          <div className="flex items-center justify-between gap-3">
            <h2 className="font-display text-lg font-semibold tracking-tight">Ontraport</h2>
            <StatusPill configured={ontraportConfigured} />
          </div>
          <p className="mt-0.5 text-sm text-muted-foreground">API credentials and what happens to pushed leads.</p>
        </div>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">API credentials</CardTitle>
            <CardDescription>From your Ontraport account under Administration → Integrations → API.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-5">
            <div className="space-y-1.5">
              <Label htmlFor="appId">App ID</Label>
              <Input
                id="appId"
                value={appId}
                onChange={(e) => setAppId(e.target.value)}
                className="font-mono"
                placeholder="Api-Appid"
                autoComplete="off"
                spellCheck={false}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="apiKey">API Key</Label>
              <SecretInput id="apiKey" value={apiKey} onChange={setApiKey} placeholder="Api-Key" />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Push mapping</CardTitle>
            <CardDescription>Suitable leads are added to these, if set.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-5">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label htmlFor="tag">Tag ID</Label>
                <Input id="tag" inputMode="numeric" value={tagId} onChange={(e) => setTagId(e.target.value)} className="font-mono" placeholder="optional" />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="seq">Sequence ID</Label>
                <Input id="seq" inputMode="numeric" value={sequenceId} onChange={(e) => setSequenceId(e.target.value)} className="font-mono" placeholder="optional" />
              </div>
            </div>

            <div className="flex items-center justify-between rounded-lg border px-4 py-3">
              <div>
                <div className="text-sm font-medium">Auto-push on completion</div>
                <div className="text-xs text-muted-foreground">Push suitable leads automatically when a search finishes.</div>
              </div>
              <Switch checked={autoPush} onCheckedChange={setAutoPush} />
            </div>
          </CardContent>
        </Card>

        <div className="flex justify-end">
          <Button type="submit" disabled={ontraportBusy}>{ontraportBusy ? 'Saving…' : 'Save Ontraport'}</Button>
        </div>
      </form>

      {/* 2Captcha */}
      <form onSubmit={saveCaptcha} className="space-y-4">
        <div>
          <div className="flex items-center justify-between gap-3">
            <h2 className="font-display text-lg font-semibold tracking-tight">2Captcha</h2>
            <StatusPill configured={captchaConfigured} />
          </div>
          <p className="mt-0.5 text-sm text-muted-foreground">Solves the reCAPTCHA on the ASIC registry search.</p>
        </div>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">API credentials</CardTitle>
            <CardDescription>From your 2Captcha account dashboard.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-5">
            <div className="space-y-1.5">
              <Label htmlFor="captchaKey">API Key</Label>
              <SecretInput
                id="captchaKey"
                value={captcha.apiKey ?? ''}
                onChange={(v) => setCaptcha((c) => ({ ...c, apiKey: v }))}
                placeholder="2Captcha API key"
              />
            </div>
          </CardContent>
        </Card>

        <div className="flex justify-end">
          <Button type="submit" disabled={captchaBusy}>{captchaBusy ? 'Saving…' : 'Save 2Captcha'}</Button>
        </div>
      </form>

      {/* Reverse WHOIS (WhoisXML) */}
      <form onSubmit={saveWhoisXml} className="space-y-4">
        <div>
          <div className="flex items-center justify-between gap-3">
            <h2 className="font-display text-lg font-semibold tracking-tight">Reverse WHOIS</h2>
            <StatusPill configured={whoisXmlConfigured} />
          </div>
          <p className="mt-0.5 text-sm text-muted-foreground">
            Finds a business's domains by ABN for the "Find websites &amp; contacts" stage.
          </p>
        </div>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">API credentials</CardTitle>
            <CardDescription>The WhoisXML API key (Domains Research Suite), from your whoisxmlapi.com account.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-5">
            <div className="space-y-1.5">
              <Label htmlFor="whoisXmlKey">API Key</Label>
              <SecretInput
                id="whoisXmlKey"
                value={whoisXmlKey}
                onChange={setWhoisXmlKey}
                placeholder="WhoisXML API key"
              />
            </div>
          </CardContent>
        </Card>

        <div className="flex justify-end">
          <Button type="submit" disabled={whoisXmlBusy}>{whoisXmlBusy ? 'Saving…' : 'Save Reverse WHOIS'}</Button>
        </div>
      </form>

      {/* ASIC connection */}
      <form onSubmit={saveAsic} className="space-y-4">
        <div>
          <div className="flex items-center justify-between gap-3">
            <h2 className="font-display text-lg font-semibold tracking-tight">ASIC connection</h2>
            <StatusPill
              configured={forceTls13}
              onLabel="TLS 1.3"
              offLabel="TLS 1.3 off"
              title={forceTls13 ? 'Handshake matches a browser' : 'Cloudflare will likely refuse these requests'}
            />
          </div>
          <p className="mt-0.5 text-sm text-muted-foreground">How searches reach ASIC, and how hard they push.</p>
        </div>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">TLS 1.3 only</CardTitle>
            <CardDescription>
              ASIC is behind Cloudflare, which inspects how the connection is opened. Offering
              older TLS versions gets the request flagged as a bot and refused with 403 before
              it's read — measured on this server, off succeeded 1 time in 10, on succeeded 10
              in 10. Leave this on unless ASIC stops supporting TLS 1.3.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex items-center gap-3">
              <Switch id="forceTls13" checked={forceTls13} onCheckedChange={setForceTls13} />
              <Label htmlFor="forceTls13" className="font-normal">
                {forceTls13 ? 'On — recommended' : 'Off — searches will likely fail'}
              </Label>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Proxy</CardTitle>
            <CardDescription>
              ASIC sits behind Cloudflare, which blocks server IPs once they look like a scraper — after
              that every record fails with 403. Routing through a residential or mobile proxy keeps
              enrichment working. Leave blank to connect directly from this server.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-5">
            <div className="space-y-1.5">
              <Label htmlFor="proxyUrl">Address</Label>
              <Input
                id="proxyUrl"
                value={proxyUrl}
                onChange={(e) => setProxyUrl(e.target.value)}
                placeholder="http://gate.provider.com:7000"
                className="font-mono"
                autoComplete="off"
                spellCheck={false}
              />
              <p className="text-xs text-muted-foreground">
                Supports http, https, socks4, socks4a and socks5. Credentials may be embedded
                (<span className="font-mono">http://user:pass@host:port</span>) or set below.
              </p>
            </div>

            <div className="grid gap-5 sm:grid-cols-2">
              <div className="space-y-1.5">
                <Label htmlFor="proxyUsername">Username</Label>
                <Input
                  id="proxyUsername"
                  value={proxyUsername}
                  onChange={(e) => setProxyUsername(e.target.value)}
                  placeholder="Optional"
                  className="font-mono"
                  autoComplete="off"
                  spellCheck={false}
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="proxyPassword">Password</Label>
                <SecretInput
                  id="proxyPassword"
                  value={proxyPassword}
                  onChange={setProxyPassword}
                  placeholder="Optional"
                />
              </div>
            </div>

            <div className="flex flex-wrap items-center gap-3">
              <Button type="button" variant="outline" onClick={testAsic} disabled={testBusy}>
                {testBusy ? 'Testing…' : 'Test connection'}
              </Button>
              {testResult && (
                <p
                  className={cn(
                    'text-sm',
                    testResult.succeeded ? 'text-emerald-700 dark:text-emerald-400' : 'text-destructive',
                  )}
                >
                  {testResult.message}
                </p>
              )}
            </div>
            <p className="text-xs text-muted-foreground">
              Tests the values above without saving them — run this before starting a large search.
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Parallel ASIC sessions</CardTitle>
            <CardDescription>
              How many records are enriched at the same time. Higher is faster but uses more server
              resources and increases the chance ASIC throttles requests — capped at {concurrencyLimit}.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-1.5">
              <Label htmlFor="concurrency">Sessions (1–{concurrencyLimit})</Label>
              <Input
                id="concurrency"
                type="number"
                inputMode="numeric"
                min={1}
                max={concurrencyLimit}
                value={maxConcurrency}
                onChange={(e) => setMaxConcurrency(e.target.value)}
                className="w-28 font-mono"
              />
              <p className="text-xs text-muted-foreground">Applies from the next search run — a run already in progress keeps its current setting.</p>
            </div>
          </CardContent>
        </Card>

        <div className="flex justify-end">
          <Button type="submit" disabled={asicBusy}>{asicBusy ? 'Saving…' : 'Save ASIC settings'}</Button>
        </div>
      </form>
    </div>
  )
}
