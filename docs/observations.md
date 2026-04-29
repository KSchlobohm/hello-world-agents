# Observations: Single Agent vs. Multi-Agent Workflow

## Experiment

Using the Microsoft Agent Framework (v1.3.0), I ran the same prompt through two configurations:

1. **Writer only** — a single `ChatClientAgent` with creative writing instructions
2. **Writer → Editor** — a sequential workflow where the Writer's output is passed to an Editor agent for refinement

Both used `gpt-4o-mini` via GitHub Models with the same prompt:
> "Write a short story about a haunted house."

## Writer-Only Output

- Longer and more meandering — the story spans "days and weeks" of the protagonist returning to the haunted house
- More direct prose with some repetition ("heart racing", "gathered her courage")
- Loose pacing that doesn't build tension as tightly
- Exposition-heavy — detours into backstory and setup
- The emotional payoff arrives late after extended padding

## Writer → Editor Output

- Tighter pacing — the entire adventure takes place in a single night
- More vivid sensory language ("cobblestone path", "fiery shades of red and gold", "cascaded like stardust")
- Stronger narrative structure — clear setup, rising tension, climax (finding the locket), and resolution
- Snappier dialogue with more emotional beats woven in
- Trimmed exposition — no unnecessary filler or repetitive descriptions

## Key Takeaways

| Aspect | Writer Only | Writer → Editor |
|--------|-------------|-----------------|
| Pacing | Slow, multi-day timeline | Tight, single-night arc |
| Prose quality | Functional, some repetition | Vivid imagery, varied language |
| Structure | Loose, meandering | Clear three-act structure |
| Length | Longer (more padding) | Slightly shorter (more dense) |
| Engagement | Gradual build | Immediate hook |

## Conclusion

The Editor agent does what a good human editor does — it tightens pacing, sharpens imagery, cuts unnecessary exposition, and makes the story more immediately engaging. The multi-agent sequential workflow produces noticeably more polished output than a single agent alone, demonstrating that agent composition can meaningfully improve quality even with the same underlying model.
