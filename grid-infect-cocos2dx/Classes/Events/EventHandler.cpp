//
//  EventHandler.cpp
//  GridInfect
//
//  Created by Christopher Mahar on 4/3/14.
//
//

#include "EventHandler.h"
#include <iostream>
#include <vector>

EventHandler* EventHandler::_instance = NULL;
EventHandler* EventHandler::getInstance()
{
    if (!_instance)
    {
        _instance = new EventHandler;
    }
    return _instance;
}

std::vector<IOnChangeBoardIndex *> _onChangeBoardIndexListeners;
std::vector<IOnLevelSolved *> _onLevelSolvedListeners;
std::vector<IOnUnbindPieces *> _onUnbindPiecesListeners;

void EventHandler()
{
    //FOR NOW, ONLY 1 PER
    _onChangeBoardIndexListeners = std::vector<IOnChangeBoardIndex *>();
    _onLevelSolvedListeners = std::vector<IOnLevelSolved *>();
    _onUnbindPiecesListeners = std::vector<IOnUnbindPieces *>();
}

void EventHandler::setOnChangeBoardIndexListener(IOnChangeBoardIndex* listener) {
    _onChangeBoardIndexListeners.clear();
    _onChangeBoardIndexListeners.push_back(listener);
}

void EventHandler::setOnLevelSolvedListener(IOnLevelSolved* listener) {
    _onLevelSolvedListeners.clear();
    _onLevelSolvedListeners.push_back(listener);
}

void EventHandler::setOnUnbindPiecesListener(IOnUnbindPieces* listener) {
    _onUnbindPiecesListeners.clear();
    _onUnbindPiecesListeners.push_back(listener);
}

void EventHandler::onChangeBoardIndex(int x, int y, int value) {
    for (int i = 0; i < _onChangeBoardIndexListeners.size(); i++)
    {
        IOnChangeBoardIndex* listener = _onChangeBoardIndexListeners.at(i);
        listener->onChangeBoardIndex(x, y, value);
    }
}

void EventHandler::onLevelSolved() {
    for (int i = 0; i < _onLevelSolvedListeners.size(); i++)
    {
        IOnLevelSolved* listener = _onLevelSolvedListeners.at(i);
        listener->onLevelSolved();
    }
}

void EventHandler::onUnbindPieces() {
    for (int i = 0; i < _onUnbindPiecesListeners.size(); i++)
    {
        IOnUnbindPieces* listener = _onUnbindPiecesListeners.at(i);
        listener->onUnbindPieces();
    }
}