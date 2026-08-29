//
//  Piece.h
//  GridInfect
//
//  Created by Christopher Mahar on 4/2/14.
//
//

#ifndef __GridInfect__Piece__
#define __GridInfect__Piece__

#include "Enums.h"

class Piece {
public:
    bool placed;
    int i;
    int j;
	
    Piece(Tile tile) {
		_tile = tile;
		placed = false;
		i = -1;
		j = -1;
	}
    
    Tile getTile() {
		return _tile;
	}
    
private:
    Tile _tile;
};

#endif /* defined(__GridInfect__Piece__) */
