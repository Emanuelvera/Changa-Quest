---
name: Changa Quest
description: Sistema visual para un tablón vecinal de changas con ambientación adulta inspirada en gremios de fantasía.
status: draft
colors:
  surface-base: '#101923'
  surface-raised: '#1C2935'
  board: '#30271F'
  ink-primary: '#F4EFE5'
  ink-secondary: '#CBD0D2'
  accent: '#E5A64B'
  on-accent: '#21180D'
  success: '#67A878'
  warning: '#E1AE4B'
  danger: '#D47368'
  border: '#4B5965'
typography:
  display:
    fontFamily: Georgia
    fontSize: 28px
    fontWeight: '600'
    lineHeight: '1.25'
  body:
    fontFamily: system-ui
    fontSize: 16px
    fontWeight: '400'
    lineHeight: '1.5'
  label:
    fontFamily: system-ui
    fontSize: 13px
    fontWeight: '500'
    lineHeight: '1.4'
rounded:
  sm: 6px
  md: 12px
  full: 9999px
spacing:
  '1': 4px
  '2': 8px
  '3': 12px
  '4': 16px
  '5': 24px
  '6': 32px
components:
  button-primary:
    background: '{colors.accent}'
    foreground: '{colors.on-accent}'
    radius: '{rounded.md}'
  task-card:
    background: '{colors.surface-raised}'
    border: '{colors.border}'
    radius: '{rounded.md}'
sources:
  - {planning_artifacts}/briefs/brief-Changa-Quest-2026-10-16/brief.md
  - {planning_artifacts}/briefs/brief-Changa-Quest-2026-10-16/addendum.md
  - {planning_artifacts}/prds/prd-Changa-Quest-2026-11-01/prd.md
  - {planning_artifacts}/prds/prd-Changa-Quest-2026-11-01/addendum.md
updated: 2026-10-19
---

## Brand & Style

Changa Quest makes a practical neighborhood task board feel like a welcoming guild noticeboard. The fantasy inspiration is an atmospheric frame, not role-play: the product should feel trustworthy and adult, while remaining approachable to someone opening it for the first time. Everyday language, clear hierarchy, and a small number of visible choices take priority over decoration.

**[ASSUMPTION — visual starting point]** Use a deep, dusk-like canvas with warm paper and brass accents, echoing the supplied reference image. Keep the guild atmosphere in restrained landscape textures, framing, and material cues; do not make every screen a dark illustration. The user has not approved exact colors, a light theme, typography, or a component library.

The board should feel like a curated list of real local requests, not a game inventory. Real task details, reward, zone, and materials are more prominent than category ornament. Avoid copying any specific fantasy franchise, characters, insignia, or artwork.

## Colors

**[ASSUMPTION — provisional palette for discussion]** These semantic tokens translate the reference image into a quieter, readable starting palette; test contrast before implementation and revise with the team.

- **Night Slate (`{colors.surface-base}`)** anchors the application without requiring a full-bleed fantasy scene.
- **Raised Slate (`{colors.surface-raised}`)** separates navigation, forms, and task cards from the canvas.
- **Guild Board (`{colors.board}`)** is a warm, low-saturation surface for the central board or selected task panels. It should read as a material hint, not a photorealistic wooden texture behind every control.
- **Warm Ink (`{colors.ink-primary}`)** carries the main readable text; **Muted Ink (`{colors.ink-secondary}`)** carries supporting text and metadata.
- **Brass (`{colors.accent}`)** marks the primary action and small guild-inspired details. It is not a substitute for status color and should not decorate every card.
- **Signal colors (`{colors.success}`, `{colors.warning}`, `{colors.danger}`)** communicate task or safety states with text and iconography as well as color.
- **Borders (`{colors.border}`)** define card and input edges without heavy outlines.

Do not place small text directly over detailed imagery. Reserve image overlays for decorative areas whose content is repeated in readable text.

## Typography

**[ASSUMPTION]** Pair a familiar system sans-serif for all interface text with a restrained serif display face for occasional page headings. Use system fallbacks so the interface remains legible offline and across devices.

- Page titles may use `{typography.display}`; task names, descriptions, forms, prices, and navigation use `{typography.body}`.
- Use `{typography.label}` for concise field labels and metadata. Do not rely on all-caps text for hierarchy.
- Make the reward and task title easy to scan, but never let display typography push essential details below the first decision.
- Support text resizing without clipping or hiding controls. Final type ramp and font availability remain to be confirmed during implementation.

## Layout & Spacing

**[ASSUMPTION — responsive starting point]** Use a mobile-first, single-column flow on narrow screens and progressively add space for task cards on wider screens. The exact breakpoints, desktop navigation, and number of cards per row are not product decisions yet.

- Keep the first view focused on a clear page title, one primary action, a search/filter entry point, and a short list of nearby tasks.
- Use `{spacing.1}` through `{spacing.6}` as a provisional 4px-based rhythm. Reserve larger gaps for page sections, not between every card.
- Keep task cards visually consistent and compact enough to compare; do not compress reward, location, or materials into tiny metadata.
- On small screens, keep forms in one column and let the board scroll vertically. On wider screens, use additional columns only when titles and task facts remain readable.
- Allow long descriptions and localized ARS amounts to wrap; do not truncate information needed to decide whether to accept a task.

## Elevation & Depth

**[ASSUMPTION]** Establish hierarchy primarily through surface tone and spacing. Use a restrained border or shallow shadow for interactive cards and overlays; avoid glow, layered parallax, and game-like floating panels.

Landscape imagery may establish atmosphere on entry or in a limited hero area. It must not obscure actions, increase reading effort, or be necessary to understand where a task takes place.

## Shapes

**[ASSUMPTION]** Use modest rounded corners (`{rounded.sm}` and `{rounded.md}`) for cards, fields, and buttons. Reserve `{rounded.full}` for compact status/category chips only. Keep the board’s paper/card character through framing, not ornate borders or scroll-like panels.

## Components

Visual properties below use provisional tokens. Behavioral rules belong to [EXPERIENCE.md](./EXPERIENCE.md); that document and this design contract take precedence over any illustrative mockup.

- **Primary action** — Brass background `{colors.accent}`, high-contrast text, `{rounded.md}`. One dominant action per screen; label it with a direct verb such as “Publicar changa” or “Ver tareas”.
- **Task card** — Raised surface `{colors.surface-raised}`, `{rounded.md}`, thin `{colors.border}` border. Present task name, ARS reward, approximate zone, and materials information in a stable order. A task photo is optional and must not replace those facts.
- **Board header** — Page title and one short explanatory line, then search/filter and the primary action. Guild language may appear in decorative context, not as a puzzle the user must decode.
- **Form field** — Raised or base surface with persistent label, visible focus ring, and nearby validation text. Do not communicate errors through border color alone.
- **Status chip** — Uses the appropriate semantic state token and a readable text label. Color is supplementary, never the only status signal.
- **Category chip** — Muted surface and text; must not compete with the task title or reward.
- **Navigation item** — Clear text label and a simple icon where useful. Avoid ornate fantasy iconography that makes common actions harder to recognize.
- **Atmospheric illustration** — Use sparingly and only where it does not compete with content; avoid franchise-specific marks and details.

## Do's and Don'ts

| Do | Don't |
|---|---|
| Make tasks, rewards, zones, and materials immediately scannable | Fill the board with decorative text, badges, or multiple competing banners |
| Use a restrained guild atmosphere around a practical neighborhood service | Make the product look like a children’s game or require role-play |
| Pair each icon with a recognizable label when meaning could be unclear | Make users infer an action from a fantasy symbol alone |
| Keep a single clear primary action on each screen | Put several equally prominent calls to action above the task content |
| Treat status and safety as semantic information with text | Encode urgency, completion, or risk using color alone |
| Keep imagery subordinate to readable task facts | Use detailed backgrounds behind forms or small text |
