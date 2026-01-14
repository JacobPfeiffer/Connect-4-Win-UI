# TODO List

1. On hover of any token in the column, show the users color as the onhover view on the first available token in the column.
   1. Add opacity to the color.
2. Add a color pallete so user can select their token color.
   1. If we choose black background. Users should be disallowed from choosing black.
3. Add SQL database to store state
4. Allow users to save an restore named games.
5. Add a tournament feature with a scoreboard.
6. add keybinds so user can set tokens using up, down, left, right arrows and enter.
7. add an animation over the winining positions that loops until a new game.
8. Animate token dropping by using moving the preview position from top of the column to destination slot. use async and delay for transitions between open spots so it looks like it is dropping.
9. Check for wins in parallel
10. Create a factory for building BoardColumnViewModel