---
layout: article
title: "Kill the YAML: Type-Safe Build Pipelines in TypeScript"
categories: [oss]
tags: [oss]
---

It's been a while since I've written anything here. Talks, work, life — the blog kept losing the priority battle. But I've spent the last months building something that got me genuinely excited about writing again, so here we are: this post exists because the project demanded it.

I have spent a large part of my career in DevOps, and I have a confession: I don't trust my own pipelines. Not because they're badly written — because of *what* they're written in.

Build logic in most projects is smeared across three substrates: YAML that no tool can type-check, bash embedded in that YAML as multi-line strings, and a Makefile someone wrote in 2019 that everyone is afraid to touch. None of it has autocomplete. None of it is testable locally in any honest sense. And the debugging loop is the worst in all of software: edit YAML, commit, push, wait four minutes, read logs, repeat. We would never accept that feedback loop for application code. For build code, we've normalized it.

So I built [Zuke](https://github.com/zuke-build/zuke) — a build automation system for Deno and TypeScript where the build is just a class.

## The core idea: targets are class fields

In Zuke, you define your build in a `zuke.ts` file as a TypeScript class. Each target is a class field created with a fluent API:

```ts
import { Build, target, run } from "jsr:@zuke/core";
import { DenoTasks } from "jsr:@zuke/deno";

class MyBuild extends Build {
  clean = target()
    .executes(async () => { /* ... */ });

  restore = target()
    .executes(async () => { /* ... */ });

  compile = target()
    .dependsOn(this.clean, this.restore)
    .executes(async () => {
      await DenoTasks.check((s) => s.paths("mod.ts"));
    });
}

await run(MyBuild);
```

Notice what `dependsOn` receives: `this.clean`, not `"clean"`. That's the whole trick, and it changes everything about how the build behaves under maintenance.

In every string-based tool — GitHub Actions `needs:`, Makefile prerequisites, npm scripts calling npm scripts — dependencies are stringly-typed. Rename a target and you've silently broken every reference to it, and you'll find out at runtime, on CI, at the worst possible moment. In Zuke, targets reference each other *by field reference*. Rename `compile` with your editor's rename-symbol and every reference moves with it. A typo is a compile error. Your build graph participates in the type system like any other code.

Zuke discovers the targets, builds the dependency graph, sorts it topologically, and runs it. That's the core — deliberately small, no magic.

This model isn't my invention. It's an homage to [NUKE](https://nuke.build/), Matthias Koch's brilliant build system for .NET, which I have missed every single day since moving into the TypeScript world. Zuke is my attempt to bring those ideas to Deno.

## Your CI YAML becomes a build artifact

Here's the part I'm most opinionated about. Even with a nice local build tool, most teams maintain their CI configuration *separately*, by hand — which means the truth about the pipeline lives in two places, and they drift.

Zuke inverts this. You declare CI in the build itself:

```ts
class MyBuild extends Build {
  cicd = cicd({ provider: "github" });
  // targets...
}
```

And Zuke *generates* the GitHub Actions (or GitLab CI, or Azure Pipelines) YAML from your build definition — then verifies on CI that the committed YAML matches what the build would generate. You never hand-edit workflow files again. The YAML doesn't disappear; it stops being a source of truth and becomes what it should have been all along: a build artifact.

I've heard the objection already: "you're just moving the problem." I don't think so. The problem was never that YAML exists — it's that YAML is where the *authoring* happens, with no types, no functions, no tests, no refactoring support. Move authoring into TypeScript and YAML generation becomes as unremarkable as compiler output.

## Typed wrappers instead of man pages

Real builds shell out to tools constantly: Docker, Terraform, Playwright, kubectl, Vite. Zuke ships 30+ typed wrapper packages on [JSR](https://jsr.io/@zuke) — one per tool — so instead of assembling argument strings from memory, you get autocomplete:

```ts
await DockerTasks.build((s) =>
  s.tag("myapp:latest").file("Containerfile").context(".")
);
```

For anything unwrapped, there's a generic `@zuke/cmd` fallback and an ergonomic `$` tagged template for raw shell — with sane defaults (throw on failure, capture output). Crucially, arguments stay a discrete argv array end to end, built on `Deno.Command`, never a concatenated shell string. Command injection is structurally impossible, not merely discouraged.

## Not just CI: the boring, load-bearing CD features

Running tasks fast is the easy part. Trusting a tool with actual delivery — deployments, releases — takes more boring machinery underneath. Zuke has a lot of that machinery.

**Incremental caching.** Declare a target's inputs and outputs, and it only re-runs when they actually change:

```ts
compile = target()
  .inputs("src", "deno.json")   // re-run only when these change…
  .outputs("dist")              // …or when dist is missing
  .executes(/* … */);
```

Zuke fingerprints inputs with SHA-256 (built-in Web Crypto — still zero dependencies) and skips targets whose fingerprint matches the last successful run. Non-file inputs — a parameter, a tool version, a git commit — fold in via `.cacheKey()`. The cache is deliberately best-effort: a corrupt store is treated as empty and everything just rebuilds, so a broken cache can never break a build. And with a remote store configured, the cache is **shared across machines** — a fresh CI checkout or a teammate's clone restores outputs instead of rebuilding them. There's also `--affected` to run only targets impacted by changes since a git base, and `--parallel` for concurrent execution of independent targets.

**Durable run state.** By default a run is entirely in-memory. Opt in — `--state`, an env var, or an `HttpStateStore` for production — and every run persists a versioned JSON record: status, the exact graph it planned, resolved parameters, per-target progress with timestamps, who ran it (`--actor`), and an append-only audit trail. Kill the process mid-deploy and the record on disk shows precisely which target was running when the lights went out. Targets get `ctx.state`, a small durable key/value store, to leave metadata behind for later runs. Secrets are structurally excluded: `.secret()` parameters never enter the record, and everything written through `ctx.state` passes through a redactor first.

**Waiting, resuming, and compensations.** Real delivery pipelines don't run start-to-finish — they *park*. A target can declare a `.waitsFor()` gate (a manual approval, an external system callback), and the run suspends durably. Later — minutes or days — `zuke resume --signal <name>` delivers the signal and the run picks up where it stopped. `zuke runs` lists and inspects persisted runs; `zuke cancel` doesn't just stop a run, it executes its **compensations** — the declared undo logic for side effects like a half-finished deployment. On top of that: timeouts for runs that wait too long, locking so two machines don't run the same build, and recovery that picks up safely after a crash.

If that paragraph reads like workflow-engine territory — yes, a bit. But it's the same typed class, the same `this.deploy` references, and all of it opt-in: a plain build with none of this configured writes nothing and pays nothing.

## A build tool has to earn trust

That last point matters to me beyond ergonomics. A build tool runs inside other people's pipelines, with their secrets and their publishing credentials in scope. That makes it a supply-chain attack surface, and as someone who spends his free time in the OWASP world, I refused to build one casually.

So Zuke has **zero runtime dependencies**. Releases publish to JSR via OIDC trusted publishing with provenance. CI is least-privilege and SHA-pinned, the lockfile is frozen, and scanning — zizmor, actionlint, gitleaks, osv-scanner — runs continuously. Naturally, the scanning itself is a typed Zuke target. The [SECURITY.md](https://github.com/zuke-build/zuke/blob/master/SECURITY.md) documents the full posture, and OpenSSF Scorecard keeps me honest in public.

I'd love for "what's your build tool's supply-chain posture?" to become a normal question. Most of the ecosystem would not enjoy answering it.

## The experimental frontier: AI in the build graph

This is the part where opinions diverge, so let me be precise about what it does and doesn't do.

`@zuke/ai` makes a model a first-class citizen of the build graph in two ways. First, **AI code review as a gate**: a reviewer reads the diff and returns a *structured* verdict — score, severity, findings — that gets posted to the PR and can break the build past a threshold you choose. Typed output, not a blob of prose.

"AI review" itself is nothing new, of course — Copilot review, CodeRabbit, and half a dozen others already comment on your PRs. The difference is *where it lives and what it can do*. Those are platform features: advisory prose bolted onto GitHub, configured in a web UI, powerless to actually stop anything, and gone the moment you move to GitLab. Zuke's reviewer is a **build target**. It's defined in the same typed TypeScript as the rest of your pipeline, versioned and code-reviewed like any other build change, sequenced in the graph (run it after the tests pass, not in parallel with a broken build), and it works identically on GitHub, GitLab, Azure — or on your laptop before you ever push. And because its verdict is structured data rather than a comment thread, it can be a real quality gate with a threshold you tune, sharing the same token budget and audit trail as everything else in the build. Copilot review is a feature of your forge; this is a feature of your *pipeline*.

Second, **self-healing targets**. Attach `.recoverWith(aiFixer(...))` to any target, and when it fails, the fixer diagnoses the failure from the error output and the diff, then posts a committable, Copilot-style suggestion to the PR. The default is diagnose-only — it writes no files. If you explicitly opt in to auto-apply, edits are gated behind a path allowlist and a file cap, and a fix only *counts* when the real command re-runs green. A token budget caps spend across every reviewer and fixer.

Should an AI ever touch your build? I'm genuinely still forming my own answer, which is why every default is conservative. But "my CI diagnosed its own failure and handed me a one-click fix" has already saved me real time, and I think structured, budgeted, verify-before-you-believe is the only responsible shape for this feature.

One more thing worth saying plainly, because it's in the README too: much of Zuke itself was written with AI assistance — then reviewed, type-checked under strict rules (no `any`, no `as`), and held to a 95% coverage gate that CI enforces. I'd rather tell you how the sausage is made and let the rigor speak for itself.

## Built for the agent era

There's a second, quieter consequence of living in an agentic world: it's no longer just humans who read your docs and write your builds. Coding agents do too — and most tools leave them to guess. Zuke treats agents as a first-class audience, in three layers:

**Skills, so agents author builds correctly.** Zuke ships two agent skills — `zuke-setup` (scaffold Zuke into a project) and `zuke-write-build` (write or edit a `zuke.ts`) — authored as portable `SKILL.md` folders following the open [Agent Skills](https://agentskills.io) standard. The repo carries marketplace metadata for Claude Code, Codex, and Gemini CLI, so the same skills install on all three (for Claude Code it's `/plugin marketplace add zuke-build/zuke`). Once installed, they trigger automatically when you ask the agent to add Zuke to a project or write a build — steering it toward the typed `*Tasks` wrappers instead of hallucinating APIs or shelling out.

**An MCP server, so agents operate builds safely.** `zuke mcp` serves your build over the Model Context Protocol: an agent can discover targets, inspect the graph, and — only if you allow it — run them. The permissions are deliberately paranoid: running targets is off unless you pass `--allow-run` (optionally with a glob allow-list), sensitive targets can be gated behind an operator token with `--protect`, destructive runs can require explicit confirmation, and every tool call lands in the run record's audit trail with time, actor, and outcome. An agent managing your pipeline is useful; an unaccountable one is a incident report.

**LLM-ready documentation.** The repo ships [`llms.txt`](https://github.com/zuke-build/zuke/blob/master/llms.txt) and `llms-full.txt` — the complete typed surface of every package in one file, following the emerging llms.txt convention. Point any model at it and it has the exact API instead of a plausible-sounding approximation. The difference between an agent that guesses and an agent that knows is a context file.

I don't think this is a gimmick. If agents are going to write and run builds — and they already are — then "agent ergonomics" is just ergonomics, and the same principles apply: give them types, give them least privilege, and leave an audit trail.

## What Zuke is not (yet)

Honesty section. Zuke requires Deno as the runtime — it can happily drive builds for Node/npm projects, but Deno must be installed. There's no plugin ecosystem yet (the [extension contract](https://github.com/zuke-build/zuke/blob/master/docs/extending.md) exists; the ecosystem doesn't). And CI generation covers the common pipeline shapes, not every exotic feature of every provider. If your workflow is 80% `workflow_dispatch` matrix wizardry, you'll still be writing some YAML.

## Try it

If you have Deno installed, you're three commands away:

```sh
deno install -A -g -n zuke jsr:@zuke/cli   # once
zuke setup                                  # scaffolds zuke.ts + launchers
./zuke                                      # run the build
```

Everything lives at [github.com/zuke-build/zuke](https://github.com/zuke-build/zuke) and [zuke.build](https://zuke.build). It's MIT, it builds itself with itself, and contributions are very welcome — the tool wrappers in particular are nicely scoped first contributions: pick a CLI you love, follow the pattern, ship a typed wrapper.

And if you build for .NET: go use [NUKE](https://github.com/nuke-build/nuke). Tell Matthias I sent you.
