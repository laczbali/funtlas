# funtlas

The goal of this app is to find roads that are fun to drive on (read: twisty, good condition, etc).
It uses data from Open Street Map, with the Overpass API.

# (Planned) User Flow
1. User selects a country from a dropdown
2. App queries and shows a list of areas within that country (eg: North, South, etc)
3. User adjusts the admin level for the area query, if desired, then picks an area

(alternative flow is that the user searches for an area name at the start, and picks from results)

4. App donwloads the roads (prim, sec, tert) of that area, builds an SQLite DB (name is normalized area name)
5. User is presented with a distinct list of tag names. When clicked, it lists the distict values for it.
6. User can apply filters based on tags (has, doesn't have, equals, not equals)
7. User is presented with a list of roads that match, with their assigned score
8. User adjusts scoring wheights, if needed
