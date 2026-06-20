---
name: mr-fire-drill-simulator
description: >
  Project context and build conventions for a Mixed Reality fire-drill training
  app on Meta Quest 3 (Meta XR SDK, passthrough). Use this skill whenever working
  on ANYTHING related to this project — Unity/XR scripts, the Create/Run mode
  logic, spawnable hazard objects, the scenario JSON save/load round-trip, the
  end-of-run API call, the Next.js + Supabase dashboard, the pitch/demo copy, or
  task planning. Trigger it even when the user just says "the fire drill app",
  "the simulator", "Create mode", "Run mode", "the dashboard", or references
  scenario JSON, extinguishers, alarms, or endpoints, even if they don't name the
  project explicitly. It is the single source of truth for architecture, the data
  formats, and what is in-scope vs. future work.
---

# MR Fire Drill Simulator

A Mixed Reality fire-drill training application for the **Meta Quest 3**, built
with the **Meta XR SDK** (passthrough mixed reality). Started as a 12-hour
hackathon build at the AIBoomi hackathon, Bangalore, June 2026.

## The one-liner

An **architecture-agnostic** fire-drill training app that lets an organization
author and run realistic fire drills inside their *own* physical space in mixed
reality — no pre-built virtual level required. The headset uses the real room as
the stage; users place hazards into their actual scanned space and then run
timed, scored drills against them, with results sent to a dashboard.

"A flight simulator, but for fire safety — and it works in any building."

## Why this exists (problem framing)

Fire drills are mandatory almost everywhere (offices, factories, hospitals,
schools) but broken: they're scheduled (no surprise), boring (nobody takes them
seriously), and produce **no data** — no manager can say who knew where the
extinguisher was, who froze, or how long evacuation took. This app makes drills
realistic, repeatable, and **measurable**. The game is the hook; the measurable
readiness data is the product.

---

## Core architecture

Two modes operate on the **same scanned room**. The defining principle:

> **Architecture-agnostic.** The app never relies on a static, pre-built 3D level.
> It scans the real environment and places everything relative to that scan.
> Avoid any design that hardcodes a specific building layout.

(When talking to non-technical / VC audiences, say "works in any building, out
of the box" — never the phrase "architecture-agnostic", which reads as *software*
architecture to engineers and means nothing to everyone else.)

### Create mode — the authoring tool

The safety officer / author designs a drill for their specific space.

- **"As easy as Figma"** is the UX north star: drag/place items onto the scanned
  room, no scripting required by the end user.
- Uses the **Meta XR SDK spawning / hand-tracking component** for click-to-spawn.
  Do **not** hand-roll placement logic; lean on the SDK component.
- **One spawn object, reused.** Build a single spawnable item, parameterized by
  type, rather than a separate code path per item. Every hazard/item is the same
  kind of authorable, scriptable object — "fire should be scriptable just like
  alarms."
- Output: a saved scenario (JSON) tied to that physical space.

### Run mode — the drill

Run mode **just plays the same scene** that Create mode authored, with a timer.

- Loads the scenario JSON and **restores every object at the exact same position
  in the scanned room** where it was authored. This positional fidelity across
  Create → Run is the single biggest technical risk; de-risk it first (see
  "Persistence" below).
- Times the run and tracks the player's actions.
- On reaching the endpoint, fires **one API call** to the backend with the
  performance details.

---

## The spawnable item system

Implement a single `SpawnableItem` (prefab + component) driven by a type enum.
This is the backbone of both modes.

```
SpawnableItemType:
  - Alarm
  - ExtinguisherA      // Class A extinguisher
  - ExtinguisherB      // Class B extinguisher
  - FireA              // Class A fire (matches ExtinguisherA)
  - FireB              // Class B fire (matches ExtinguisherB)
  - Endpoint           // safe exit / muster point — carries the end-of-run API trigger
```

Scope note on fire/extinguisher classes: **A and B only.** There is no Class I /
Class C in scope — earlier notes mentioning it are superseded.

**Correct-extinguisher matching** is a scored mechanic: using ExtinguisherA on
FireA (and B on B) is "correct"; mismatches should be detectable for scoring.

**The Endpoint is special.** It is the object that carries the API trigger.
Reaching the endpoint is the single event that fires the end-of-run POST. Keep
exactly one place in the code that sends the POST — the endpoint's "reached"
event — so the contract surface stays tiny.

### Assets needed (hackathon shopping list)

Grab free assets early; do not model anything from scratch.

- 2 fire extinguishers (Class A, Class B) — visually distinguishable
- 2 fire types (matching A / B)
- 1 alarm asset

---

## Persistence: the scenario JSON round-trip

This is the part to build and test **first**, even with a single hardcoded
object, before building out the full item set or any UI.

- Create mode **serializes** placed items to JSON: at minimum each item's
  **type** and its **transform/position** (relative to the scanned room anchor).
- Run mode **rehydrates** that JSON and places each item back at the **identical
  position** in the scanned room.
- Store the run + scenario data in JSON. The end-to-end sequence is:

```
Action → reach endpoint → store run result as JSON → one API call to backend
```

**The de-risk milestone:** Run mode loads a JSON and places ONE object back at
the exact scanned-room position it was authored at. Once that works, the hard
problem is solved and the rest is repetition.

> Field-level details of the scenario JSON (exact key names, how the room anchor
> is referenced) are not yet frozen. Lock them before writing the serializer, and
> record them here once decided.

---

## End-of-run API contract

When the player reaches the endpoint, the headset POSTs the run result to the
backend. This is the **interface boundary** between the XR app (Person A) and the
dashboard backend (Person B) — once frozen, the two halves are fully decoupled
and can be built in parallel against this shape.

**Draft payload (field names NOT yet finalized — confirm with the backend owner
before implementing, then update this block):**

```json
{
  "run_id": "uuid",
  "scenario_id": "string",
  "player_name": "string",
  "completed": true,
  "time_seconds": 84.2,
  "actions": {
    "alarm_triggered": true,
    "correct_extinguisher_used": true,
    "reached_endpoint": true
  },
  "score": 920,
  "timestamp": "iso8601"
}
```

Rules for this contract:
- It is **frozen once agreed.** Every collision in a two-person build comes from
  this interface drifting. Write it down, both owners agree, then don't touch it.
- The headset only **POSTs at end of run** — it never reads from the backend.
  This keeps the simulation from ever blocking on the dashboard.

---

## Dashboard (backend — to be built later)

Decoupled from the headset entirely. Proposed stack:

- **Supabase** stores run records (score, time, scenario ID, player/org).
- **Next.js** frontend pulls and displays scores / leaderboards / per-run detail
  on its own cadence, deployable to a public URL (Vercel) for the demo.

The dashboard is what reframes the project from "VR toy" to "compliance & risk
tool" — invest in making it look credible.

> The Supabase schema (table + exact column names) and the receiving API route
> are **not yet defined.** They must match the end-of-run payload above. Define
> them, then record `schema.sql` and the route here. Until then, treat this whole
> section as a plan, not settled fact.

---

## Scope: in vs. future

**In scope (hackathon):**
- Passthrough MR + room scan
- Create mode (spawn/place items) and Run mode (play + time + score)
- Scenario JSON save/load round-trip
- End-of-run API call (POST only)
- Fire/extinguisher classes A and B; alarm; endpoint

**Future notes (explicitly OUT of scope — do not build now, but design so as not
to preclude):**
- **SAM 3 object segmentation** — recognizing probable fire-risk areas from a
  live camera feed in real time; auto-placing hazards in the most probable spots
  given the architecture (e.g. electrical fire in the lift, gas fire in the
  pantry).
- **LLM context-awareness** of building architecture — reasoning about where
  disasters are most likely, grounded in government fire-safety advisories;
  eventual "Fire Expert" agent that auto-generates scenarios.
- **Object-recognition pipeline / data collection** for architecture and a
  training benchmark (BTS).
- **Heat-haze fire shader** (red→green line / refraction effect) — marked
  experimental; nice-to-have visual, not core.
- **Replay → render in MR** — replaying a recorded run back in mixed reality.
- The full backend/dashboard (Supabase + Next.js) if the 12 hours run short.

---

## Build conventions & task division

For a 2-person, 12-hour build with **one headset**:

- **Person A — XR / Unity (headset-bound):** Create mode, Run mode, scene
  interaction, scoring, the mocked POST.
- **Person B — Backend + Dashboard (headset-free):** Supabase, the results API,
  the Next.js dashboard. Builds against mock data so the single headset is never
  a bottleneck.

Three rules that keep a two-person build from colliding:

1. **Run mode before Create mode.** Hardcode object positions first and prove the
   loop (see fire → grab correct extinguisher → fire out → reach exit → done).
   Create mode is the cut candidate if time runs out — a runnable drill you can't
   author still demos well; an authorable drill that won't run does not.
2. **Person B is never blocked** — everything runs against mock data until
   integration (~hour 10).
3. **Freeze the API contract at hour 1, touch it never again.**

Integration is a single sync point near the end: Person A swaps the mocked POST
URL for Person B's real endpoint. If the contract held, it's a one-line change.

---

## Pitch language (for demo / submission copy)

Keep all of this jargon-light. Lead with the problem (fire drills are useless),
then reveal the tech.

**Elevator (~20s):** Every office runs fire drills and treats them as a joke —
walk outside, stand around, walk back in. Nobody learns anything and there's zero
data on whether people would survive a real fire. We built a fire drill that
happens *in your actual office*: put on a headset, see your real workspace — now
with smoke, alarms, and spreading fire — and find the extinguisher, hit the
alarm, get to the exit. We score every second. **A flight simulator, but for fire
safety, and it works in any building.**

**Key reframes for Q&A:**
- "Isn't this just a game?" → The dashboard. The game is the hook; measurable
  readiness data is the product (compliance & risk-management tool).
- "Why now?" → Headsets just got good and cheap enough (Quest 3 passthrough is
  genuinely usable).
- "Why will anyone buy it?" → Fire safety is *already* legally mandated. You're
  not creating demand, you're upgrading a forced purchase — budget already exists.

**Business framing:** Low-cost recurring SaaS; the only customer cost is the
headset (one-time, per site). Moat = distribution. Patent angle: software tied to
a specific computer architecture may be patentable (TODO: file patents). Path:
Development & validation → product-market fit (marketing) → distribution. TAM:
industrial (factories, warehouses), office (corporate, coworking), restaurants,
educational, residential (builders), sports complexes, government institutions.

---

## Open questions (unresolved — flag, don't paper over)

1. Is the simulation realistic enough to deliver real training value?
2. How do we drive headset adoption with orgs / consumers?
3. How do we scale to ~100 employees with a single headset?

---

## Maintaining this skill

This file is the source of truth. When the team locks down anything currently
marked "not yet finalized" — the scenario JSON field names, the exact API payload
keys, the Supabase schema — **update the relevant section here** so a fresh Claude
session (or a teammate's) starts from the real spec, not a draft. As the backend
and reference material grow, split them into sibling reference files
(`api-contract.md`, `scenario-format.md`, `schema.sql`, `pitch.md`) and point to
them from here, keeping this SKILL.md under ~500 lines.
