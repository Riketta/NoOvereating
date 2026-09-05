# No Overeating

Stops pawns and animals from eating a piece of food when that last piece would overflow their stomach and the extra nutrition would be wasted.

## How it works

- An adult ibex at 0.06/0.32 food with rice (0.05 nutrition per grain) available eats 5 grains, not 6 - vanilla takes the 6th and throws away the part that does not fit.
- Both vanilla piece counts are floored instead of rounded up, with a minimum of one piece; all amounts are read from the actual food, the eater and its current food need at runtime.
- When every available food overflows anyway (say, a simple meal at 0.9 nutrition), the pawn still eats one unit: overeating is acceptable when it is unavoidable.
- Covers every vanilla eating path: humans and animals, maps and caravans, feeding patients, bottle-feeding babies, administering food as medicine. DLC and modded foods work automatically.

Steam Workshop: [No Overeating](https://steamcommunity.com/sharedfiles/filedetails/?id=3794993524).
