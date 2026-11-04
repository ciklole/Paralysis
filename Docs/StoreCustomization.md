# Store customization

The Store scene is authored by hand and is treated as a protected scene. These
notes cover the Inspector-friendly controls that already exist; they do not
require rebuilding the scene.

## Grocery placement

Each grocery is a normal scene object with a `GroceryItem` component. Move its
Transform in the Scene view to change where it sits. The pickup prompt uses the
object's Transform unless an optional `Interaction Point` is assigned.

Selecting a grocery shows two extra buttons in the Inspector:

- **Snap to surface below** - raycasts down from the object and places it on
  the nearest surface (with Undo support). Useful for shelf placement.
- **Select interaction point** - selects the assigned interaction point, or
  the grocery itself when none is assigned.

The `Item Id` must match an id in the `ShoppingCart` list (`bread`, `milk`,
`eggs` in the current story). A warning appears when it is empty.

## Cashier models

`CashierInteraction` exposes:

- **Normal Model** - the ordinary cashier child object.
- **Transformed Model** - the scary/transformed child object (optional).
- **Animator**, **Idle Trigger**, **Transformation Trigger**,
  **Restore Trigger** - optional animation hooks.
- **Restore After Transformation** - development default; turn it off for a
  permanent ending later.

Selecting the cashier shows two extra buttons:

- **Auto-assign from children** - finds child objects by common names
  (`Normal`, `Human`, `Cashier`, `Clerk`, `Scary`, `Transformed`, `Creepy`,
  `Monster`), or falls back to the direct children with renderers. It also
  assigns the first child Animator when one is found.
- **Validate setup** - reports missing models, models that are the component
  object itself, the same object used for both views, or a missing Animator.

Both models must be child objects, not the object that holds
`CashierInteraction`, otherwise the sequence would disable itself.

## Dialogue and audio

The checkout dialogue, scanner clips, transformation sound, failure sound and
ambience source are all serialized on `CashierInteraction`. Empty optional
references are skipped rather than throwing.

The normal and transformed speaker colours and the line where the cashier
starts sounding wrong are also Inspector fields.
