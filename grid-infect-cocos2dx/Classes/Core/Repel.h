//
//  Repel.h
//  GridInfect
//
//  Created by Christopher Mahar on 4/2/14.
//
//

#ifndef __GridInfect__Repel__
#define __GridInfect__Repel__

#include <iostream>
#include "Enums.h"

class Repel {
public:
    Repel(int _i, int _j, Tile _direction) {
		i = _i;
		j = _j;
		direction = _direction;
	}
	
    int i;
    int j;
    Tile direction;
};

#endif /* defined(__GridInfect__Repel__) */
