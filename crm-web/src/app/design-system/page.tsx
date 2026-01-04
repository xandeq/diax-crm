import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { ArrowRight, Star } from "lucide-react"

export default function DesignSystemPage() {
  return (
    <div className="min-h-screen bg-background p-10 space-y-16">
      <div className="space-y-4">
        <Badge variant="section">Design System</Badge>
        <h1 className="text-5xl font-serif">Minimalist <span className="gradient-text">Modern</span></h1>
        <p className="text-xl text-muted-foreground max-w-2xl">
          A design system that embraces modern web layouts and dynamic interactions while honoring minimalist foundations.
        </p>
      </div>

      <section className="space-y-8">
        <h2 className="text-3xl font-serif">Buttons</h2>
        <div className="flex flex-wrap gap-4 items-center">
          <Button>Primary Button <ArrowRight className="ml-2 h-4 w-4" /></Button>
          <Button variant="secondary">Secondary</Button>
          <Button variant="outline">Outline</Button>
          <Button variant="ghost">Ghost</Button>
          <Button variant="destructive">Destructive</Button>
          <Button variant="link">Link Button</Button>
        </div>
        <div className="flex flex-wrap gap-4 items-center">
          <Button size="lg">Large Button</Button>
          <Button size="default">Default</Button>
          <Button size="sm">Small</Button>
          <Button size="icon"><Star className="h-4 w-4" /></Button>
        </div>
      </section>

      <section className="space-y-8">
        <h2 className="text-3xl font-serif">Badges</h2>
        <div className="flex flex-wrap gap-4">
          <Badge>Default</Badge>
          <Badge variant="secondary">Secondary</Badge>
          <Badge variant="outline">Outline</Badge>
          <Badge variant="destructive">Destructive</Badge>
          <Badge variant="section">Section Label</Badge>
        </div>
      </section>

      <section className="space-y-8">
        <h2 className="text-3xl font-serif">Inputs</h2>
        <div className="grid max-w-sm gap-4">
          <Input type="email" placeholder="Email address" />
          <Input type="password" placeholder="Password" />
          <div className="flex gap-2">
             <Input placeholder="Search..." />
             <Button>Search</Button>
          </div>
        </div>
      </section>

      <section className="space-y-8">
        <h2 className="text-3xl font-serif">Cards</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <Card>
            <CardHeader>
              <CardTitle>Standard Card</CardTitle>
              <CardDescription>This is a standard card description.</CardDescription>
            </CardHeader>
            <CardContent>
              <p>Card content goes here. It uses the default shadow and hover effects.</p>
            </CardContent>
            <CardFooter>
              <Button variant="outline" className="w-full">Action</Button>
            </CardFooter>
          </Card>

          <div className="rounded-xl bg-gradient-to-br from-accent via-accent-secondary to-accent p-[2px]">
            <Card className="h-full border-0">
              <CardHeader>
                <CardTitle>Featured Card</CardTitle>
                <CardDescription>With gradient border effect.</CardDescription>
              </CardHeader>
              <CardContent>
                <p>This card uses a gradient border wrapper to highlight important content.</p>
              </CardContent>
              <CardFooter>
                <Button className="w-full">Primary Action</Button>
              </CardFooter>
            </Card>
          </div>

          <Card className="bg-foreground text-background border-foreground">
            <CardHeader>
              <CardTitle className="text-background">Inverted Card</CardTitle>
              <CardDescription className="text-slate-400">Dark mode aesthetic.</CardDescription>
            </CardHeader>
            <CardContent>
              <p>Useful for high contrast sections or highlighting specific data points.</p>
            </CardContent>
            <CardFooter>
              <Button variant="secondary" className="w-full">Light Action</Button>
            </CardFooter>
          </Card>
        </div>
      </section>

      <section className="space-y-8">
        <h2 className="text-3xl font-serif">Typography</h2>
        <div className="space-y-4 border p-8 rounded-xl">
            <h1 className="text-5xl font-serif">Heading 1 (Calistoga)</h1>
            <h2 className="text-4xl font-serif">Heading 2 (Calistoga)</h2>
            <h3 className="text-3xl font-serif">Heading 3 (Calistoga)</h3>
            <h4 className="text-2xl font-semibold">Heading 4 (Inter Semibold)</h4>
            <p className="text-lg">Large body text (Inter). The quick brown fox jumps over the lazy dog.</p>
            <p className="text-base">Base body text (Inter). The quick brown fox jumps over the lazy dog.</p>
            <p className="text-sm text-muted-foreground">Small text (Inter). Used for captions and descriptions.</p>
            <p className="font-mono text-sm">Monospace text (JetBrains Mono). Used for code and technical labels.</p>
        </div>
      </section>
    </div>
  )
}
