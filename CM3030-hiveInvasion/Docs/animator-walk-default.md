# Animator controllers: Walk as default and keeping changes in git

Context
- Vendor Animator Controllers live under AlienBugsPack and are ignored by git.
- You set Walk as the default state in each bug controller so enemies visibly walk when moving.
- Because those controllers are ignored, your changes won’t persist across machines unless you duplicate them to a tracked folder and reassign them on prefabs.

What we added
- Editor menu: Tools → Animators → Duplicate + Reassign (no graph changes) for all 4 bugs
  - Duplicates vendor controllers to Assets/Controllers/:
    - Game_Bug_101.controller (Swarm)
    - Game_Bug_104.controller (Armored)
    - Game_Bug_201.controller (Enemy)
    - Game_Bug_302.controller (Ranged)
  - Reassigns the Enemy/Armored/Ranged/Swarm prefabs to use those duplicates.
  - Your manual default state (Walk) is preserved because we copy the asset as-is.

One‑time steps
1) In Unity, run Tools → Animators → Duplicate + Reassign (no graph changes) for all 4 bugs.
2) Commit the new controllers under Assets/Controllers and the prefab reference changes.

Optional tool
- Tools → Animators → Analyze all 4 bug controllers (unconditional Idle/Walk)
  - Reports if vendor graphs contain unconditional transitions that could snap back to Idle/Walk.
- Tools → Animators → Duplicate + Fix (prune unconditional Idle/Walk) for all 4 bugs
  - Creates duplicates and prunes unconditional Any State → Idle/Walk and Idle↔Walk transitions so CrossFade can drive locomotion. Use only if needed.

Notes
- Animator Controller edits save immediately; reverting prefab/scene changes won’t revert the controller. Use version control to undo controller edits or re‑copy the vendor asset.
- If you later change the vendor controllers, rerun the duplicate step to refresh the copies and recommit.

