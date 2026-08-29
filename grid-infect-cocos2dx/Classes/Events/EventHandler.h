//
//  EventHandler.h
//  GridInfect
//
//  Created by Christopher Mahar on 4/3/14.
//
//

#ifndef __GridInfect__EventHandler__
#define __GridInfect__EventHandler__

#include <iostream>
#include "IOnChangeBoardIndex.h"
#include "IOnLevelSolved.h"
#include "IOnUnbindPieces.h"

class EventHandler
{
public:
    static EventHandler* getInstance();
    
    void setOnChangeBoardIndexListener(IOnChangeBoardIndex* listener);
    void setOnLevelSolvedListener(IOnLevelSolved* listener);
    void setOnUnbindPiecesListener(IOnUnbindPieces* listener);
    void onChangeBoardIndex(int i, int j, int value);
    void onLevelSolved();
    void onUnbindPieces();
    
private:
    EventHandler(){};
    EventHandler(EventHandler const&){};
    void operator=(EventHandler const&){};
    static EventHandler* _instance;
};

#endif /* defined(__GridInfect__EventHandler__) */
