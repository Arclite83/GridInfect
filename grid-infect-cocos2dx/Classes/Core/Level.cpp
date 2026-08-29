//
//  Level.cpp
//  GridInfect
//
//  Created by Christopher Mahar on 4/1/14.
//
//

#include "Piece.h"
#include "Level.h"

bool _solved;

Level::Level(int level)
{
    Pieces = new std::vector<Piece *>();
    setSolved(false);
    
    initByLevel(level);
}

Level::Level()
{
    Pieces = new std::vector<Piece *>();
    setSolved(false);
    
    int board_temp[] = {
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
    
    for (int i = 0; i < Level::Height*Level::Width; i++)
    {
        Board[i] = board_temp[i];
    }
}

bool Level::isSolved()
{
    return _solved;
}

void Level::setSolved(bool solved)
{
    _solved = solved;
}

void Level::initByLevel(int level)
{
    switch (level)
    {
        case 0:
        {
            Pieces->push_back(new Piece(R));
            Pieces->push_back(new Piece(D));
            int board_temp[] = {
                0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 1:
        {
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(D));
            int board_temp[] = {
                0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0,
                0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0,
                0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0,
                0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0,
                0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0};
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
			break;
        }
        case 2: {
			Pieces->push_back(new Piece(R));
			Pieces->push_back(new Piece(D));
			Pieces->push_back(new Piece(L));
			int board_temp[] = {
                0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                0, 1, 1, 1, 0, 0, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 1, 1, 1, 0, 1, 1, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 3: {
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(L));
			int board_temp[] = {
                0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 1, 1, 1, 0, 0, 0,
                0, 1, 1, 1, 1, 0, 1, 1, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 4: {
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(R));
			Pieces->push_back(new Piece(L));
			int board_temp[] = {
                0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 1, 1, 2, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 5: {
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(LD));
			int board_temp[] = {
                0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0,
                0, 0, 0, 1, 2, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 6: {
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(D));
			Pieces->push_back(new Piece(R));
			int board_temp[] = {
                0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0,
                0, 0, 1, 1, 1, 0, 1, 1, 0, 0, 0,
                0, 0, 1, 0, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 7: {
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(U));
			int board_temp[] = {
                0, 0, 0, 1, 1, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 1, 0, 1, 1, 0, 0, 0,
                0, 0, 0, 1, 1, 0, 2, 1, 0, 0, 0,
                0, 0, 0, 1, 1, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 1, 2, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 8: {
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(LUD));
			int board_temp[] = {
                0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0,
                0, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 1, 1, 0, 1, 1, 1, 1, 1, 1, 0,
                0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0,
                0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 9: {
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(LD));
			int board_temp[] = {
                0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0,
                0, 0, 0, 3, 1, 1, 1, 1, 1, 0, 0,
                0, 0, 0, 1, 1, 1, 1, 0, 1, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 10: {
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(LD));
			int board_temp[] = {
                0, 0, 1, 1, 1, 0, 1, 1, 1, 0, 0,
                0, 0, 0, 1, 1, 0, 2, 1, 1, 0, 0,
                0, 0, 0, 1, 0, 0, 3, 1, 1, 0, 0,
                0, 0, 1, 1, 1, 0, 1, 1, 1, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 11: {
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(RU));
			int board_temp[] = {
                0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 12: {
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(L));
			int board_temp[] = {
                0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0,
                0, 0, 0, 3, 1, 0, 1, 3, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 1, 1, 1, 0, 1, 1, 1, 0, 0,
                0, 0, 1, 1, 1, 0, 1, 1, 1, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 13: {
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(LUD));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(LU));
			int board_temp[] = {
                0, 0, 0, 0, 1, 1, 1, 1, 1, 0, 0,
                0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 1, 1, 1, 1, 2, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 14: {
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(R));
			Pieces->push_back(new Piece(L));
			int board_temp[] = {
                0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 1, 1, 0, 0, 1, 1, 1, 1, 0,
                0, 0, 1, 1, 0, 0, 1, 1, 1, 3, 0,
                0, 3, 1, 1, 0, 0, 1, 1, 1, 1, 0,
                0, 1, 1, 1, 0, 0, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 15: {
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(LU));
			int board_temp[] = {
                0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0,
                0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0,
                0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0,
                0, 1, 1, 1, 1, 1, 1, 3, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 16: {
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(RU));
			int board_temp[] = {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 1, 2, 1, 1, 1, 1, 1, 1, 0,
                1, 2, 1, 1, 1, 1, 1, 2, 1, 0, 0,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 17: {
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(LU));
			int board_temp[] = {
                0, 0, 0, 0, 1, 0, 1, 1, 1, 0, 0,
                0, 0, 0, 1, 1, 0, 1, 1, 1, 0, 0,
                0, 0, 0, 1, 1, 0, 1, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 1, 1, 1, 0, 0,
                0, 0, 0, 1, 1, 0, 0, 0, 1, 0, 0,
                0, 0, 0, 1, 1, 0, 1, 1, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 18: {
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(RD));
			int board_temp[] = {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 0,
                0, 3, 1, 1, 2, 1, 1, 1, 1, 1, 0,
                0, 1, 1, 1, 1, 1, 1, 2, 1, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 19: {
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(LU));
			int board_temp[] = {
                0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 1, 1, 3, 0,
                0, 1, 1, 1, 2, 1, 1, 1, 1, 1, 0,
                0, 0, 1, 1, 3, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 1, 1, 2, 1, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 20: {
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(L));
			int board_temp[] = {
                0, 0, 1, 0, 1, 1, 1, 0, 1, 0, 0,
                0, 1, 1, 0, 1, 1, 1, 0, 1, 1, 0,
                0, 1, 2, 0, 1, 1, 1, 0, 2, 1, 0,
                0, 1, 1, 0, 1, 1, 1, 0, 1, 1, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 21: {
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(LU));
			int board_temp[] = {
                0, 0, 0, 1, 0, 1, 1, 1, 1, 0, 0,
                0, 0, 3, 1, 0, 1, 1, 2, 1, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0,
                0, 0, 2, 1, 0, 0, 3, 1, 1, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 22: {
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(LUD));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(L));
			int board_temp[] = {
                0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 1, 1, 0, 0, 2, 0, 0, 0,
                0, 0, 0, 1, 1, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 1, 1, 1, 2, 1, 0, 0, 0,
                0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 23: {
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(LD));
			int board_temp[] = {
                0, 0, 3, 1, 2, 0, 0, 1, 1, 0, 0,
                0, 0, 1, 1, 1, 0, 0, 2, 1, 0, 0,
                0, 0, 1, 1, 1, 0, 0, 1, 1, 0, 0,
                0, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0,
                0, 0, 3, 1, 2, 0, 0, 1, 1, 0, 0,
                0, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 24: {
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(LD));
			int board_temp[] = {
                0, 0, 0, 0, 0, 1, 1, 0, 1, 0, 0,
                0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0,
                0, 0, 0, 0, 3, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 25: {
			Pieces->push_back(new Piece(L));
			Pieces->push_back(new Piece(U));
			int board_temp[] = {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                0, 3, 1, 1, 1, 1, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 26: {
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(RU));
			int board_temp[] = {
                0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0,
                0, 0, 0, 3, 1, 1, 1, 0, 0, 0, 0,
                0, 0, 0, 3, 1, 1, 1, 3, 0, 0, 0,
                0, 0, 0, 0, 1, 1, 1, 3, 0, 0, 0,
                0, 0, 0, 3, 1, 1, 1, 0, 0, 0, 0,
                0, 0, 0, 0, 3, 3, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 27: {
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(RD));
			int board_temp[] = {
                0, 0, 0, 1, 1, 2, 1, 1, 0, 0, 0,
                0, 0, 0, 1, 2, 3, 1, 0, 0, 1, 0,
                0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0, 2, 1, 0,
                0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 28: {
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(LRUD));
			int board_temp[] = {
                0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 5, 1, 1, 1, 5, 0, 0, 0,
                0, 0, 0, 5, 1, 1, 1, 5, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 29: {
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(R));
			int board_temp[] = {
                0, 0, 0, 5, 0, 0, 1, 0, 0, 0, 0,
                0, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0,
                0, 1, 0, 1, 1, 5, 1, 3, 0, 1, 0,
                0, 0, 0, 0, 1, 1, 1, 1, 2, 1, 0,
                0, 0, 0, 1, 0, 0, 0, 1, 1, 1, 0,
                0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 30: {
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(R));
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(LU));
			int board_temp[] = {
                0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 1, 0, 0, 1, 1, 1, 0, 0, 0, 0,
                0, 1, 0, 1, 1, 5, 1, 0, 0, 0, 0,
                0, 0, 1, 0, 5, 1, 1, 0, 1, 0, 0,
                0, 0, 1, 5, 1, 1, 0, 0, 1, 0, 0,
                0, 0, 1, 1, 1, 2, 0, 0, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 31: {
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(RU));
			int board_temp[] = {
                0, 0, 1, 1, 0, 1, 0, 2, 1, 0, 1,
                0, 0, 0, 0, 0, 0, 1, 0, 3, 1, 1,
                0, 0, 1, 2, 1, 1, 0, 1, 0, 0, 0,
                0, 1, 3, 0, 0, 5, 1, 1, 1, 0, 0,
                0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 1,
                0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 32: {
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(D));
			int board_temp[] = {
                0, 1, 0, 1, 1, 1, 1, 2, 1, 0, 0,
                0, 0, 0, 3, 1, 1, 1, 1, 1, 1, 0,
                0, 0, 0, 0, 1, 1, 1, 0, 1, 0, 0,
                0, 0, 0, 0, 1, 5, 1, 0, 1, 0, 0,
                0, 0, 0, 5, 1, 1, 1, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 33: {
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(R));
			Pieces->push_back(new Piece(L));
			int board_temp[] = {
                0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0,
                0, 0, 0, 1, 1, 1, 1, 1, 1, 0, 0,
                0, 0, 0, 1, 1, 1, 0, 1, 5, 0, 0,
                0, 0, 5, 1, 1, 1, 1, 1, 1, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 34: {
			Pieces->push_back(new Piece(L));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(RD));
			int board_temp[] = {
                0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0,
                0, 1, 0, 1, 1, 1, 1, 5, 0, 0, 0,
                0, 0, 1, 0, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 35: {
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(RU));
			int board_temp[] = {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 1, 0, 1, 2, 1, 0, 1, 1, 0,
                0, 0, 0, 5, 1, 1, 1, 5, 1, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 1, 0, 1, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 36: {
			Pieces->push_back(new Piece(LUD));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(RD));
			int board_temp[] = {
                0, 0, 1, 1, 1, 0, 1, 1, 1, 0, 0,
                0, 0, 1, 1, 5, 0, 1, 1, 0, 0, 0,
                0, 0, 1, 1, 5, 0, 5, 1, 0, 0, 0,
                0, 0, 0, 1, 1, 0, 1, 0, 1, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 37: {
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(RD));
			int board_temp[] = {
                0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0,
                0, 0, 0, 2, 1, 1, 1, 0, 1, 0, 0,
                0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0,
                0, 0, 0, 1, 5, 1, 1, 0, 0, 0, 0,
                0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 38: {
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(RU));
			int board_temp[] = {
                0, 0, 0, 1, 1, 1, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 1, 1, 0, 1, 1, 0, 0,
                0, 0, 0, 2, 0, 1, 5, 1, 1, 0, 0,
                0, 0, 5, 1, 1, 1, 0, 0, 1, 0, 0,
                0, 0, 0, 1, 5, 0, 0, 1, 1, 0, 0,
                0, 0, 0, 1, 1, 1, 0, 1, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 39: {
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(LUD));
			Pieces->push_back(new Piece(LD));
			int board_temp[] = {
                0, 0, 0, 0, 0, 5, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0,
                0, 0, 0, 1, 1, 1, 1, 5, 0, 0, 0,
                0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 40: {
			Pieces->push_back(new Piece(R));
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(D));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(LD));
			int board_temp[] = {
                0, 0, 0, 1, 1, 0, 1, 1, 1, 0, 0,
                0, 1, 0, 0, 0, 0, 5, 1, 1, 0, 0,
                5, 1, 0, 1, 1, 0, 1, 1, 1, 0, 0,
                0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                0, 1, 0, 1, 0, 0, 0, 1, 1, 0, 0,
                0, 5, 0, 2, 1, 1, 1, 1, 1, 1, 1 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 41: {
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(R));
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(RD));
			int board_temp[] = {
                0, 0, 0, 0, 0, 0, 1, 1, 0, 1, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                1, 0, 1, 1, 1, 5, 0, 0, 0, 0, 0,
                0, 0, 1, 1, 1, 1, 0, 1, 1, 5, 0,
                0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0,
                0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 42: {
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(L));
			int board_temp[] = {
                0, 0, 1, 1, 1, 0, 0, 1, 1, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 1, 5, 0, 0,
                1, 1, 1, 0, 1, 1, 1, 1, 0, 1, 1,
                0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0,
                1, 0, 1, 0, 1, 0, 1, 1, 1, 0, 1,
                0, 5, 1, 0, 0, 0, 1, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 43: {
			Pieces->push_back(new Piece(UD));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(LU));
			int board_temp[] = {
                0, 0, 0, 1, 0, 1, 1, 1, 5, 0, 0,
                0, 0, 0, 0, 5, 1, 1, 0, 1, 0, 0,
                0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0,
                0, 0, 0, 0, 5, 1, 5, 1, 1, 0, 0,
                0, 0, 0, 0, 1, 1, 1, 1, 1, 0, 0,
                0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 44: {
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(D));
			int board_temp[] = {
                0, 0, 5, 0, 0, 0, 0, 0, 1, 0, 0,
                0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,
                1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 0,
                0, 0, 1, 0, 0, 0, 1, 0, 1, 0, 0,
                0, 0, 1, 0, 0, 1, 1, 0, 0, 0, 0,
                0, 0, 5, 0, 0, 1, 1, 1, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 45: {
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(LR));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(L));
			int board_temp[] = {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 1, 1, 1, 1, 0, 1, 5, 0, 0,
                0, 0, 1, 0, 1, 1, 1, 0, 0, 1, 0,
                1, 0, 1, 1, 0, 2, 1, 0, 0, 0, 0,
                1, 5, 1, 0, 1, 1, 0, 1, 0, 1, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 46: {
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(LRD));
			int board_temp[] = {
                0, 5, 1, 1, 1, 0, 1, 1, 1, 0, 0,
                0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0,
                0, 1, 1, 0, 1, 0, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 1, 5, 1, 0, 0, 0, 0,
                0, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 47: {
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(UD));
			Pieces->push_back(new Piece(LR));
			Pieces->push_back(new Piece(U));
			int board_temp[] = {
                0, 1, 0, 1, 0, 1, 1, 1, 1, 1, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0,
                0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0,
                0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 1, 5, 0, 0, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 48: {
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(LR));
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(LUD));
			int board_temp[] = {
                1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1,
                0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0,
                0, 0, 1, 0, 1, 0, 2, 1, 1, 0, 0,
                0, 0, 1, 1, 1, 0, 0, 1, 1, 1, 0,
                0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0,
                0, 0, 5, 1, 0, 0, 0, 0, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 49: {
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(D));
			Pieces->push_back(new Piece(LU));
			int board_temp[] = {
                0, 0, 0, 1, 1, 1, 1, 0, 2, 1, 0,
                0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0,
                0, 0, 5, 1, 1, 1, 1, 1, 0, 1, 0,
                0, 0, 0, 1, 0, 1, 0, 1, 0, 0, 0,
                1, 1, 0, 1, 0, 1, 1, 0, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 50: {
			Pieces->push_back(new Piece(D));
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(L));
			int board_temp[] = {
                0, 0, 5, 1, 0, 1, 1, 5, 0, 0, 0,
                0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0,
                0, 0, 3, 1, 1, 1, 1, 0, 1, 1, 0,
                1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1,
                0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 51: {
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(L));
			Pieces->push_back(new Piece(LRD));
			int board_temp[] = {
                0, 0, 1, 0, 5, 0, 0, 0, 0, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 1, 0, 1, 0,
                0, 0, 1, 1, 1, 1, 0, 0, 1, 5, 0,
                0, 1, 1, 0, 0, 1, 0, 1, 0, 0, 0,
                1, 1, 1, 0, 1, 0, 1, 0, 0, 0, 0,
                0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 52: {
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(UD));
			int board_temp[] = {
                0, 0, 0, 0, 5, 1, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 1, 1, 0, 1, 0, 0, 0,
                0, 0, 1, 1, 1, 1, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 1, 0, 1, 1, 1, 1, 0,
                0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 1, 0, 1, 1, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 53: {
			Pieces->push_back(new Piece(LR));
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(RU));
			int board_temp[] = {
                0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 1, 2, 1, 1, 0, 0,
                0, 0, 0, 0, 1, 1, 1, 0, 1, 1, 0,
                5, 1, 0, 1, 1, 1, 1, 0, 1, 1, 5,
                0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0,
                1, 1, 0, 0, 1, 1, 1, 1, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 54: {
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(LRD));
			int board_temp[] = {
                0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0,
                0, 0, 1, 0, 0, 1, 1, 0, 1, 1, 0,
                0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0,
                0, 1, 1, 1, 0, 1, 1, 0, 1, 5, 0,
                0, 0, 0, 1, 1, 0, 1, 0, 0, 0, 0,
                0, 0, 0, 0, 1, 1, 5, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 55: {
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(LD));
			int board_temp[] = {
                0, 0, 5, 0, 0, 1, 1, 1, 0, 1, 0,
                1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0,
                0, 1, 0, 1, 1, 1, 1, 0, 1, 1, 0,
                0, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0,
                0, 0, 1, 0, 0, 1, 2, 0, 0, 0, 0,
                0, 0, 1, 0, 1, 0, 1, 1, 0, 1, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 56: {
			Pieces->push_back(new Piece(LUD));
			Pieces->push_back(new Piece(UD));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(RD));
			int board_temp[] = {
                0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0,
                0, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0,
                0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0,
                1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0,
                0, 0, 1, 0, 0, 1, 1, 0, 0, 0, 0,
                0, 0, 1, 1, 0, 1, 5, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 57: {
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(LD));
			int board_temp[] = {
                0, 1, 1, 1, 1, 0, 1, 1, 1, 1, 0,
                0, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0,
                1, 0, 1, 0, 1, 1, 1, 5, 1, 0, 0,
                0, 0, 0, 1, 1, 1, 1, 1, 1, 0, 1,
                0, 0, 0, 1, 5, 0, 1, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 1, 1, 0, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 58: {
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(D));
			Pieces->push_back(new Piece(LUD));
			int board_temp[] = {
                0, 1, 0, 0, 5, 1, 0, 1, 1, 0, 0,
                0, 1, 0, 1, 0, 1, 1, 1, 1, 1, 1,
                0, 0, 0, 0, 1, 1, 2, 1, 0, 0, 0,
                0, 1, 0, 0, 1, 1, 0, 5, 0, 0, 0,
                0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 59: {
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(LUD));
			Pieces->push_back(new Piece(LU));
			int board_temp[] = {
                0, 0, 0, 1, 0, 1, 0, 1, 2, 1, 0,
                0, 0, 0, 5, 1, 0, 1, 5, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 1, 0, 1, 0,
                0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0,
                0, 0, 0, 5, 1, 1, 1, 5, 0, 0, 0,
                0, 0, 0, 1, 1, 0, 0, 1, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 60: {
			Pieces->push_back(new Piece(R));
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(LUD));
			Pieces->push_back(new Piece(U));
			int board_temp[] = {
                0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 1, 1, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0,
                0, 5, 1, 1, 1, 1, 1, 5, 0, 0, 0,
                0, 0, 3, 1, 1, 1, 1, 1, 1, 1, 1,
                0, 0, 0, 1, 0, 1, 1, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 61: {
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(D));
			Pieces->push_back(new Piece(L));
			int board_temp[] = {
                0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0,
                0, 1, 0, 1, 1, 1, 1, 1, 0, 1, 0,
                0, 2, 0, 0, 1, 0, 1, 0, 0, 0, 0,
                0, 1, 0, 0, 2, 0, 1, 0, 0, 0, 0,
                0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0,
                0, 5, 1, 1, 0, 1, 1, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 62: {
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(L));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(U));
			int board_temp[] = {
                0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0,
                0, 1, 1, 0, 1, 1, 0, 1, 1, 5, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 1, 0, 0, 1, 1, 0, 0, 1, 0, 0,
                0, 1, 1, 0, 1, 1, 0, 1, 0, 1, 5,
                0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 63: {
			Pieces->push_back(new Piece(LUD));
			Pieces->push_back(new Piece(L));
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(RD));
			int board_temp[] = {
                0, 0, 1, 1, 0, 0, 1, 0, 1, 0, 0,
                1, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0,
                0, 0, 1, 5, 0, 1, 2, 0, 0, 0, 0,
                0, 1, 0, 1, 0, 1, 1, 0, 1, 1, 0,
                0, 0, 1, 0, 0, 1, 1, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 64: {
			Pieces->push_back(new Piece(L));
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(LU));
			int board_temp[] = {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 1, 1, 1, 1, 1, 5, 1, 0, 0, 0,
                0, 1, 0, 0, 1, 0, 1, 1, 1, 0, 0,
                5, 1, 0, 1, 1, 1, 1, 1, 1, 0, 0,
                0, 0, 0, 0, 0, 0, 5, 1, 1, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 65: {
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(LU));
			int board_temp[] = {
                0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 1, 0, 1, 0,
                0, 0, 1, 0, 0, 1, 1, 1, 1, 0, 0,
                0, 0, 1, 0, 0, 5, 1, 0, 1, 0, 0,
                5, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 66: {
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(D));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(L));
			int board_temp[] = {
                0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0,
                0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0,
                0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0,
                0, 0, 0, 1, 2, 1, 1, 0, 5, 0, 0,
                0, 0, 0, 1, 1, 1, 0, 0, 1, 1, 1 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 67: {
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(LUD));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(R));
			int board_temp[] = {
                0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0,
                0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0,
                5, 1, 0, 1, 1, 1, 0, 1, 0, 1, 0,
                1, 0, 1, 1, 0, 1, 1, 1, 1, 0, 0,
                0, 0, 1, 1, 1, 1, 0, 5, 1, 0, 0,
                0, 5, 1, 1, 0, 1, 1, 1, 0, 1, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 68: {
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(U));
			int board_temp[] = {
                0, 0, 1, 1, 0, 1, 1, 1, 0, 1, 0,
                0, 0, 0, 0, 1, 0, 0, 5, 0, 0, 0,
                0, 0, 1, 0, 0, 1, 0, 1, 0, 0, 0,
                0, 0, 0, 0, 1, 1, 0, 1, 0, 1, 0,
                0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0,
                0, 0, 1, 0, 1, 5, 0, 1, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 69: {
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(D));
			int board_temp[] = {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 1, 1, 0, 0,
                0, 0, 5, 1, 1, 1, 0, 1, 1, 0, 0,
                0, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0,
                0, 0, 0, 1, 0, 1, 0, 2, 0, 0, 0,
                0, 0, 0, 1, 0, 1, 0, 1, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 70: {
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(D));
			int board_temp[] = {
                0, 0, 0, 1, 5, 0, 0, 1, 0, 0, 0,
                0, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 1, 1, 0, 0, 0, 1, 5, 0, 0,
                0, 0, 1, 0, 1, 1, 0, 0, 1, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 71: {
			Pieces->push_back(new Piece(LUD));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(R));
			int board_temp[] = {
                0, 1, 1, 0, 1, 0, 1, 0, 0, 0, 0,
                0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0,
                0, 1, 1, 0, 1, 5, 1, 0, 0, 0, 0,
                0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0,
                0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 72: {
			Pieces->push_back(new Piece(LUD));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(D));
			Pieces->push_back(new Piece(L));
			int board_temp[] = {
                0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0,
                1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0,
                0, 0, 0, 1, 0, 1, 1, 1, 0, 0, 0,
                0, 0, 5, 1, 1, 0, 1, 1, 1, 1, 0,
                1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1,
                0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 73: {
			Pieces->push_back(new Piece(L));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(U));
			int board_temp[] = {
                0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0,
                0, 1, 1, 0, 1, 1, 1, 1, 1, 0, 1,
                1, 0, 1, 1, 1, 1, 1, 1, 1, 5, 0,
                0, 0, 0, 0, 1, 5, 1, 0, 1, 0, 0,
                0, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 74: {
			Pieces->push_back(new Piece(L));
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(R));
			Pieces->push_back(new Piece(LD));
			int board_temp[] = {
                0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0,
                0, 0, 1, 1, 1, 0, 0, 1, 1, 1, 0,
                0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0,
                0, 0, 1, 0, 5, 1, 0, 0, 1, 1, 1 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 75: {
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(L));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(RUD));
			int board_temp[] = {
                5, 1, 1, 1, 1, 0, 1, 1, 0, 0, 0,
                0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 1, 1, 1, 0, 1, 0, 1,
                0, 0, 1, 1, 5, 0, 0, 1, 0, 0, 0,
                0, 0, 1, 1, 0, 0, 0, 2, 0, 0, 0,
                0, 0, 1, 1, 1, 1, 0, 1, 1, 1, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 76: {
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(R));
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(RD));
			int board_temp[] = {
                0, 0, 0, 0, 0, 1, 0, 1, 1, 1, 0,
                0, 0, 1, 5, 0, 1, 0, 1, 0, 0, 0,
                1, 1, 1, 0, 0, 1, 0, 1, 1, 0, 0,
                0, 0, 1, 0, 1, 1, 1, 1, 0, 0, 0,
                0, 1, 1, 0, 1, 0, 1, 0, 5, 0, 1,
                0, 0, 1, 0, 0, 1, 1, 1, 1, 0, 1 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 77: {
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(R));
			Pieces->push_back(new Piece(LUD));
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(L));
			int board_temp[] = {
                0, 0, 1, 1, 1, 1, 1, 1, 0, 1, 0,
                0, 1, 1, 0, 1, 1, 1, 5, 1, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0,
                0, 1, 0, 1, 0, 0, 1, 1, 1, 1, 0,
                0, 5, 1, 1, 1, 1, 0, 0, 1, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 78: {
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(L));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(D));
			int board_temp[] = {
                1, 0, 1, 1, 1, 0, 1, 0, 0, 0, 0,
                0, 1, 0, 0, 0, 0, 0, 5, 0, 0, 0,
                1, 1, 0, 1, 1, 1, 1, 0, 5, 1, 0,
                0, 1, 0, 1, 1, 1, 0, 1, 1, 1, 0,
                0, 1, 0, 0, 0, 1, 0, 1, 0, 0, 0,
                0, 5, 0, 0, 0, 1, 0, 1, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 79: {
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(R));
			Pieces->push_back(new Piece(L));
			Pieces->push_back(new Piece(U));
			int board_temp[] = {
                0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 0,
                0, 5, 1, 1, 1, 1, 1, 5, 0, 0, 0,
                0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0,
                1, 0, 0, 1, 1, 1, 1, 1, 1, 5, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 80: {
			Pieces->push_back(new Piece(R));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(LUD));
			int board_temp[] = {
                0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0,
                0, 0, 0, 1, 1, 0, 1, 1, 1, 1, 1,
                0, 1, 0, 1, 0, 1, 1, 1, 1, 1, 0,
                0, 0, 1, 0, 1, 1, 2, 0, 0, 1, 0,
                0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 0,
                0, 0, 0, 1, 0, 0, 1, 0, 0, 5, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 81: {
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(L));
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(D));
			int board_temp[] = {
                0, 1, 0, 0, 0, 1, 5, 0, 0, 0, 0,
                0, 1, 5, 0, 0, 1, 1, 0, 0, 0, 0,
                1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 1, 0, 0, 0, 1, 1, 1, 1, 0,
                0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0,
                0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 82: {
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(D));
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(U));
			int board_temp[] = {
                0, 1, 0, 0, 5, 1, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 1, 1, 0, 1, 1, 0, 0,
                0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 1,
                0, 0, 0, 0, 1, 1, 1, 1, 1, 0, 1,
                0, 1, 0, 1, 0, 1, 1, 0, 1, 1, 1,
                0, 0, 0, 0, 1, 5, 0, 0, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 83: {
			Pieces->push_back(new Piece(LUD));
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(D));
			int board_temp[] = {
                0, 0, 1, 0, 5, 0, 0, 1, 0, 0, 0,
                0, 1, 1, 0, 1, 0, 0, 0, 0, 1, 0,
                1, 1, 1, 1, 1, 0, 0, 1, 5, 1, 0,
                0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                0, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0,
                1, 0, 0, 1, 1, 0, 1, 1, 1, 1, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 84: {
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(LR));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(LU));
			int board_temp[] = {
                0, 0, 1, 1, 0, 1, 0, 0, 0, 1, 0,
                0, 0, 0, 1, 1, 1, 0, 1, 2, 1, 0,
                1, 0, 1, 1, 0, 1, 0, 1, 1, 0, 0,
                0, 0, 0, 0, 1, 1, 0, 1, 1, 1, 1,
                0, 0, 0, 5, 1, 1, 0, 0, 1, 5, 0,
                1, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 85: {
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(R));
			int board_temp[] = {
                0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 5, 1, 1, 0, 1, 0, 0, 0,
                0, 1, 0, 1, 1, 1, 0, 1, 1, 1, 1,
                0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1,
                0, 0, 0, 1, 1, 2, 0, 0, 0, 0, 0,
                0, 0, 0, 5, 5, 1, 1, 1, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 86: {
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(L));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(LRUD));
			int board_temp[] = {
                0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0,
                0, 0, 1, 0, 0, 1, 1, 1, 1, 0, 0,
                0, 0, 1, 1, 1, 1, 0, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 0, 0, 1, 1, 0, 1,
                5, 1, 1, 1, 1, 2, 1, 0, 2, 0, 0,
                0, 0, 1, 0, 0, 0, 0, 1, 1, 1, 1 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 87: {
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(LUD));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(D));
			int board_temp[] = {
                0, 1, 0, 1, 0, 0, 1, 5, 0, 1, 0,
                0, 1, 0, 0, 0, 0, 1, 1, 0, 1, 0,
                0, 1, 0, 1, 0, 1, 1, 0, 0, 1, 0,
                0, 1, 0, 1, 0, 0, 1, 1, 1, 1, 0,
                0, 1, 0, 0, 1, 0, 1, 1, 5, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 88: {
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(D));
			Pieces->push_back(new Piece(LRUD));
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(RUD));
			int board_temp[] = {
                0, 1, 0, 1, 0, 1, 1, 5, 1, 0, 0,
                0, 0, 1, 0, 1, 1, 1, 1, 1, 5, 0,
                0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0,
                0, 1, 0, 0, 0, 1, 1, 1, 0, 0, 0,
                0, 1, 0, 0, 0, 1, 1, 1, 1, 0, 0,
                0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 89: {
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(LD));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(LUD));
			Pieces->push_back(new Piece(RUD));
			int board_temp[] = {
                0, 0, 1, 1, 1, 1, 0, 1, 5, 0, 0,
                0, 0, 0, 1, 1, 0, 1, 1, 0, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 1, 1, 1, 1, 1, 1, 0, 0,
                0, 0, 5, 1, 1, 1, 1, 1, 0, 1, 5,
                0, 0, 0, 0, 1, 5, 1, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 90: {
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(L));
			Pieces->push_back(new Piece(LR));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(RD));
			int board_temp[] = {
                0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1,
                0, 1, 0, 1, 1, 0, 1, 0, 2, 1, 0,
                0, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0,
                0, 1, 5, 0, 1, 1, 0, 1, 1, 1, 0,
                0, 0, 0, 1, 1, 1, 5, 0, 0, 0, 0,
                0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 91: {
			Pieces->push_back(new Piece(LR));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(LD));
			int board_temp[] = {
                0, 5, 1, 1, 0, 0, 1, 0, 0, 0, 0,
                0, 5, 1, 0, 0, 1, 1, 0, 1, 0, 1,
                0, 1, 1, 1, 1, 0, 1, 1, 1, 0, 1,
                1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 0,
                0, 0, 1, 5, 0, 5, 1, 0, 1, 0, 0,
                0, 1, 1, 0, 0, 1, 1, 1, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 92: {
			Pieces->push_back(new Piece(RD));
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(LRU));
			int board_temp[] = {
                0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0,
                0, 1, 1, 0, 1, 0, 1, 1, 1, 0, 0,
                0, 1, 1, 1, 1, 1, 1, 1, 0, 5, 0,
                0, 0, 1, 5, 1, 1, 1, 1, 2, 0, 1,
                0, 0, 0, 0, 1, 0, 1, 1, 0, 0, 1,
                1, 0, 1, 0, 1, 0, 1, 1, 1, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 93: {
			Pieces->push_back(new Piece(RU));
			Pieces->push_back(new Piece(LR));
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(R));
			Pieces->push_back(new Piece(LUD));
			int board_temp[] = {
                0, 0, 0, 1, 5, 5, 5, 5, 0, 0, 1,
                5, 1, 0, 1, 1, 1, 1, 1, 0, 1, 1,
                0, 0, 0, 1, 1, 1, 0, 1, 0, 1, 1,
                1, 1, 1, 1, 0, 2, 1, 0, 0, 0, 1,
                0, 1, 1, 0, 0, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 94: {
			Pieces->push_back(new Piece(R));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(LU));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(LRU));
			int board_temp[] = {
                0, 0, 1, 0, 0, 5, 1, 1, 1, 0, 1,
                0, 1, 1, 0, 1, 1, 1, 1, 1, 1, 0,
                0, 0, 1, 0, 2, 1, 0, 0, 0, 0, 0,
                0, 1, 1, 1, 0, 0, 0, 1, 1, 5, 0,
                0, 0, 1, 5, 1, 0, 0, 0, 0, 0, 0,
                0, 5, 1, 1, 1, 1, 0, 1, 0, 1, 1 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 95: {
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(LR));
			Pieces->push_back(new Piece(LU));
			int board_temp[] = {
                0, 0, 0, 5, 1, 0, 5, 0, 0, 0, 0,
                1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 5,
                0, 0, 0, 1, 0, 1, 1, 1, 0, 1, 1,
                0, 0, 1, 1, 0, 1, 1, 1, 0, 1, 1,
                0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 5 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 96: {
			Pieces->push_back(new Piece(R));
			Pieces->push_back(new Piece(LRD));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(LUD));
			Pieces->push_back(new Piece(L));
			int board_temp[] = {
                1, 1, 1, 1, 1, 0, 5, 0, 0, 0, 0,
                1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 5,
                0, 1, 2, 0, 0, 1, 0, 1, 0, 0, 0,
                0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0,
                1, 1, 1, 1, 1, 0, 0, 1, 0, 1, 0,
                0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 97: {
			Pieces->push_back(new Piece(LUD));
			Pieces->push_back(new Piece(D));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(R));
			Pieces->push_back(new Piece(LU));
			int board_temp[] = {
                0, 5, 0, 0, 0, 5, 1, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 1, 1, 1, 1, 1, 1,
                0, 1, 0, 0, 0, 1, 1, 0, 0, 0, 0,
                5, 1, 0, 0, 1, 1, 0, 1, 0, 0, 0,
                0, 1, 2, 0, 0, 1, 1, 1, 0, 0, 0,
                1, 1, 1, 0, 1, 0, 1, 1, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 98: {
			Pieces->push_back(new Piece(LRU));
			Pieces->push_back(new Piece(RUD));
			Pieces->push_back(new Piece(D));
			Pieces->push_back(new Piece(U));
			Pieces->push_back(new Piece(L));
			int board_temp[] = {
                0, 0, 1, 1, 0, 0, 0, 1, 5, 1, 0,
                0, 0, 0, 1, 0, 0, 0, 1, 0, 1, 5,
                1, 1, 0, 1, 0, 1, 0, 1, 1, 1, 1,
                0, 5, 1, 1, 0, 0, 1, 1, 1, 1, 0,
                1, 1, 1, 0, 0, 1, 0, 1, 0, 1, 5,
                0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0 };
			for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
		case 99: {
            Pieces->push_back(new Piece(RU));
            Pieces->push_back(new Piece(LRU));
            Pieces->push_back(new Piece(LD));
            Pieces->push_back(new Piece(RUD));
            Pieces->push_back(new Piece(LRD));
            int board_temp[] = {
                1, 0, 1, 1, 0, 1, 0, 1, 5, 0, 0,
                1, 0, 0, 1, 2, 1, 1, 1, 1, 0, 1,
                1, 1, 0, 0, 0, 1, 0, 1, 1, 1, 5,
                1, 0, 5, 1, 0, 1, 0, 1, 1, 1, 1,
                1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1 };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 100: {
            Pieces->push_back(new Piece(LU));
            Pieces->push_back(new Piece(R));
            Pieces->push_back(new Piece(LD));
            Pieces->push_back(new Piece(D));
            Pieces->push_back(new Piece(RUD));
            int board_temp[] = {
                1, 1, 1, 0, 0, 0, 0, 1, 1, 5, 0,
                0, 0, 5, 1, 0, 0, 5, 1, 1, 0, 0,
                0, 0, 1, 1, 0, 0, 0, 1, 1, 0, 1,
                0, 5, 1, 1, 2, 0, 0, 1, 1, 0, 0,
                5, 1, 1, 1, 0, 0, 1, 0, 0, 1, 5,
                0, 1, 1, 0, 1, 1, 1, 1, 1, 5, 0 };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 101: {
            Pieces->push_back(new Piece(LUD));
            Pieces->push_back(new Piece(LU));
            Pieces->push_back(new Piece(R));
            Pieces->push_back(new Piece(D));
            Pieces->push_back(new Piece(LRD));
            int board_temp[] = {
                0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 1,
                1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1,
                0, 0, 5, 1, 1, 1, 0, 1, 1, 5, 1,
                0, 1, 1, 1, 1, 1, 0, 1, 1, 5, 1,
                1, 0, 1, 1, 0, 1, 0, 0, 1, 1, 1,
                0, 0, 0, 0, 0, 1, 5, 0, 1, 0, 0 };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 102: {
            Pieces->push_back(new Piece(RUD));
            Pieces->push_back(new Piece(LU));
            Pieces->push_back(new Piece(RU));
            Pieces->push_back(new Piece(RD));
            Pieces->push_back(new Piece(D));
            int board_temp[] = {
                0, 1, 1, 1, 0, 0, 0, 0, 0, 5, 1,
                5, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1,
                5, 1, 1, 1, 5, 0, 0, 0, 0, 0, 1,
                0, 1, 0, 0, 0, 1, 1, 1, 1, 0, 1,
                0, 1, 1, 0, 0, 1, 0, 1, 0, 5, 0,
                0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1 };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 103: {
            Pieces->push_back(new Piece(LRUD));
            Pieces->push_back(new Piece(LU));
            Pieces->push_back(new Piece(LRU));
            Pieces->push_back(new Piece(LD));
            Pieces->push_back(new Piece(R));
            int board_temp[] = {
                1, 1, 0, 1, 0, 0, 1, 1, 1, 1, 1,
                5, 0, 0, 0, 5, 1, 1, 5, 0, 0, 1,
                0, 1, 1, 1, 0, 1, 1, 0, 1, 5, 1,
                1, 0, 1, 1, 0, 1, 0, 1, 1, 1, 1,
                0, 0, 0, 1, 0, 1, 0, 1, 2, 1, 0,
                1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1 };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 104: {
            Pieces->push_back(new Piece(LRD));
            Pieces->push_back(new Piece(LD));
            Pieces->push_back(new Piece(RU));
            Pieces->push_back(new Piece(LU));
            Pieces->push_back(new Piece(LUD));
            int board_temp[] = {
                1, 0, 1, 1, 1, 1, 1, 0, 1, 1, 1,
                5, 1, 1, 1, 0, 1, 1, 1, 0, 5, 1,
                0, 0, 1, 1, 0, 1, 1, 0, 0, 0, 1,
                0, 0, 0, 1, 1, 1, 0, 1, 1, 1, 1,
                0, 0, 0, 0, 0, 1, 5, 1, 1, 1, 1,
                0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 105: {
            Pieces->push_back(new Piece(RU));
            Pieces->push_back(new Piece(U));
            Pieces->push_back(new Piece(LD));
            Pieces->push_back(new Piece(RUD));
            Pieces->push_back(new Piece(R));
            int board_temp[] = {
                1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0,
                0, 1, 1, 1, 0, 1, 0, 1, 1, 5, 0,
                0, 5, 1, 0, 1, 0, 0, 1, 1, 1, 0,
                0, 1, 1, 0, 0, 1, 5, 0, 1, 0, 5,
                0, 1, 1, 5, 0, 1, 0, 1, 1, 0, 1,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 106: {
            Pieces->push_back(new Piece(RU));
            Pieces->push_back(new Piece(LD));
            Pieces->push_back(new Piece(LRUD));
            Pieces->push_back(new Piece(LRD));
            Pieces->push_back(new Piece(L));
            int board_temp[] = {
                0, 5, 1, 1, 0, 1, 1, 1, 1, 1, 1,
                1, 1, 0, 0, 1, 1, 1, 5, 1, 0, 0,
                0, 1, 1, 1, 0, 1, 1, 1, 1, 0, 1,
                1, 0, 1, 0, 0, 1, 0, 1, 1, 5, 0,
                0, 0, 1, 1, 0, 0, 0, 1, 1, 0, 0,
                5, 0, 1, 1, 1, 1, 1, 1, 0, 1, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 107: {
            Pieces->push_back(new Piece(L));
            Pieces->push_back(new Piece(RUD));
            Pieces->push_back(new Piece(LUD));
            Pieces->push_back(new Piece(LD));
            Pieces->push_back(new Piece(LU));
            int board_temp[] = {
                1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0,
                0, 0, 5, 1, 1, 1, 1, 5, 0, 0, 0,
                0, 0, 5, 1, 0, 1, 1, 1, 1, 1, 1,
                1, 1, 0, 1, 0, 1, 0, 0, 0, 0, 5,
                5, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0,
                1, 0, 0, 1, 1, 1, 1, 5, 0, 0, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 108: {
            Pieces->push_back(new Piece(LRUD));
            Pieces->push_back(new Piece(RUD));
            Pieces->push_back(new Piece(LD));
            Pieces->push_back(new Piece(LU));
            Pieces->push_back(new Piece(RD));
            int board_temp[] = {
                0, 1, 0, 0, 1, 1, 1, 1, 1, 1, 5,
                0, 0, 5, 0, 1, 1, 0, 1, 1, 1, 1,
                0, 5, 0, 1, 1, 0, 1, 0, 2, 1, 0,
                0, 1, 1, 1, 0, 1, 0, 0, 1, 1, 0,
                0, 0, 0, 0, 1, 0, 1, 1, 1, 1, 0,
                0, 0, 0, 1, 1, 1, 0, 0, 5, 0, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 109: {
            Pieces->push_back(new Piece(LRUD));
            Pieces->push_back(new Piece(LRU));
            Pieces->push_back(new Piece(LRD));
            Pieces->push_back(new Piece(L));
            Pieces->push_back(new Piece(RU));
            int board_temp[] = {
                1, 1, 1, 0, 1, 0, 1, 1, 0, 0, 1,
                0, 1, 1, 0, 0, 0, 1, 0, 1, 0, 0,
                0, 1, 1, 1, 1, 0, 1, 1, 0, 1, 5,
                5, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1,
                5, 0, 1, 1, 0, 0, 0, 1, 0, 0, 1,
                1, 1, 1, 0, 0, 1, 0, 1, 1, 1, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 110: {
            Pieces->push_back(new Piece(D));
            Pieces->push_back(new Piece(RUD));
            Pieces->push_back(new Piece(LRUD));
            Pieces->push_back(new Piece(LU));
            Pieces->push_back(new Piece(L));
            int board_temp[] = {
                0, 1, 1, 1, 0, 1, 0, 1, 1, 1, 0,
                0, 5, 1, 0, 1, 0, 1, 0, 0, 5, 0,
                0, 0, 1, 1, 1, 0, 1, 0, 1, 0, 0,
                0, 5, 1, 1, 1, 1, 1, 1, 0, 1, 0,
                0, 0, 1, 1, 1, 0, 1, 0, 0, 0, 0,
                0, 5, 0, 1, 1, 1, 1, 5, 0, 0, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 111: {
            Pieces->push_back(new Piece(RD));
            Pieces->push_back(new Piece(LD));
            Pieces->push_back(new Piece(D));
            Pieces->push_back(new Piece(L));
            Pieces->push_back(new Piece(LRU));
            int board_temp[] = {
                0, 5, 1, 0, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 1, 0, 0, 5, 0, 0, 0, 0, 0,
                0, 0, 1, 0, 0, 1, 0, 1, 0, 5, 0,
                0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 0,
                1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0,
                1, 0, 1, 1, 1, 1, 2, 1, 0, 1, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 112: {
            Pieces->push_back(new Piece(LD));
            Pieces->push_back(new Piece(RU));
            Pieces->push_back(new Piece(R));
            Pieces->push_back(new Piece(RD));
            Pieces->push_back(new Piece(LRU));
            int board_temp[] = {
                0, 0, 1, 0, 1, 0, 1, 1, 1, 1, 0,
                0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0,
                1, 1, 1, 1, 1, 0, 1, 1, 5, 0, 0,
                0, 0, 1, 0, 1, 5, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 0, 1, 0, 1, 1, 5,
                0, 0, 0, 5, 1, 1, 1, 1, 1, 1, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 113: {
            Pieces->push_back(new Piece(L));
            Pieces->push_back(new Piece(LRU));
            Pieces->push_back(new Piece(LUD));
            Pieces->push_back(new Piece(R));
            Pieces->push_back(new Piece(LRD));
            int board_temp[] = {
                0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0,
                5, 1, 1, 0, 1, 0, 1, 0, 1, 0, 1,
                0, 0, 1, 1, 1, 1, 0, 5, 1, 0, 0,
                1, 1, 0, 1, 1, 0, 0, 0, 1, 0, 0,
                0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0,
                1, 1, 0, 0, 0, 1, 0, 0, 0, 1, 5
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 114: {
            Pieces->push_back(new Piece(R));
            Pieces->push_back(new Piece(L));
            Pieces->push_back(new Piece(LRUD));
            Pieces->push_back(new Piece(U));
            Pieces->push_back(new Piece(RU));
            int board_temp[] = {
                0, 0, 0, 0, 0, 0, 5, 0, 1, 0, 0,
                1, 0, 0, 1, 5, 0, 0, 0, 1, 0, 0,
                0, 0, 1, 0, 0, 1, 1, 1, 1, 0, 1,
                5, 1, 1, 1, 1, 0, 1, 0, 0, 0, 1,
                0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1,
                0, 0, 0, 0, 0, 5, 1, 0, 1, 1, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 115: {
            Pieces->push_back(new Piece(LUD));
            Pieces->push_back(new Piece(LRD));
            Pieces->push_back(new Piece(U));
            Pieces->push_back(new Piece(L));
            Pieces->push_back(new Piece(D));
            int board_temp[] = {
                0, 0, 0, 5, 0, 1, 0, 0, 1, 0, 0,
                0, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0,
                5, 1, 0, 1, 1, 1, 5, 0, 1, 0, 5,
                1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1,
                1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1,
                0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 116: {
            Pieces->push_back(new Piece(LRUD));
            Pieces->push_back(new Piece(LRU));
            Pieces->push_back(new Piece(LD));
            Pieces->push_back(new Piece(L));
            Pieces->push_back(new Piece(U));
            int board_temp[] = {
                1, 1, 1, 1, 1, 1, 1, 5, 1, 0, 0,
                1, 0, 1, 1, 0, 1, 1, 1, 1, 0, 0,
                0, 0, 5, 1, 0, 0, 1, 1, 1, 5, 0,
                1, 1, 1, 1, 1, 0, 1, 1, 1, 0, 0,
                5, 1, 0, 0, 0, 0, 1, 1, 1, 1, 0,
                0, 0, 0, 0, 0, 0, 1, 1, 5, 0, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 117: {
            Pieces->push_back(new Piece(RUD));
            Pieces->push_back(new Piece(LU));
            Pieces->push_back(new Piece(U));
            Pieces->push_back(new Piece(LD));
            Pieces->push_back(new Piece(RU));
            int board_temp[] = {
                0, 0, 1, 1, 0, 1, 1, 0, 1, 5, 0,
                0, 1, 1, 5, 0, 0, 0, 0, 1, 0, 1,
                5, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1,
                0, 1, 1, 0, 0, 0, 0, 0, 1, 0, 1,
                0, 0, 5, 1, 1, 0, 0, 1, 1, 1, 1,
                0, 0, 1, 0, 1, 0, 0, 0, 1, 1, 1
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 118: {
            Pieces->push_back(new Piece(U));
            Pieces->push_back(new Piece(RD));
            Pieces->push_back(new Piece(LD));
            Pieces->push_back(new Piece(LRU));
            Pieces->push_back(new Piece(LU));
            int board_temp[] = {
                0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0,
                1, 0, 1, 1, 1, 1, 1, 5, 1, 0, 0,
                1, 0, 1, 1, 1, 1, 1, 1, 1, 5, 0,
                5, 1, 0, 1, 0, 1, 1, 1, 1, 0, 0,
                1, 1, 1, 1, 0, 1, 1, 0, 1, 5, 0,
                0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 119: {
            Pieces->push_back(new Piece(LU));
            Pieces->push_back(new Piece(LRUD));
            Pieces->push_back(new Piece(U));
            Pieces->push_back(new Piece(LD));
            Pieces->push_back(new Piece(RD));
            int board_temp[] = {
                1, 0, 1, 0, 1, 0, 1, 0, 1, 1, 0,
                1, 1, 0, 1, 1, 1, 1, 0, 1, 0, 5,
                1, 0, 5, 0, 1, 0, 1, 1, 0, 0, 0,
                1, 5, 0, 0, 1, 1, 1, 1, 1, 1, 0,
                1, 1, 0, 1, 1, 0, 0, 0, 1, 0, 0,
                0, 5, 0, 0, 0, 0, 0, 1, 5, 0, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 120: {
            Pieces->push_back(new Piece(RU));
            Pieces->push_back(new Piece(RD));
            Pieces->push_back(new Piece(L));
            Pieces->push_back(new Piece(LRD));
            Pieces->push_back(new Piece(U));
            int board_temp[] = {
                1, 0, 1, 1, 1, 1, 0, 1, 1, 0, 1,
                1, 0, 1, 1, 1, 0, 0, 0, 1, 0, 1,
                1, 0, 1, 1, 0, 0, 0, 0, 0, 1, 5,
                1, 0, 1, 0, 1, 0, 1, 1, 0, 0, 0,
                5, 0, 1, 1, 2, 1, 0, 0, 1, 1, 0,
                0, 0, 1, 5, 0, 0, 0, 0, 0, 5, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 121: {
            Pieces->push_back(new Piece(RD));
            Pieces->push_back(new Piece(RUD));
            Pieces->push_back(new Piece(LRU));
            Pieces->push_back(new Piece(RU));
            Pieces->push_back(new Piece(D));
            int board_temp[] = {
                0, 0, 0, 1, 1, 0, 1, 1, 5, 0, 0,
                5, 0, 0, 1, 0, 1, 1, 0, 1, 1, 1,
                0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1,
                5, 0, 0, 1, 0, 0, 1, 0, 1, 1, 5,
                1, 1, 1, 1, 1, 0, 0, 1, 0, 1, 1
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 122: {
            Pieces->push_back(new Piece(D));
            Pieces->push_back(new Piece(RU));
            Pieces->push_back(new Piece(LRD));
            Pieces->push_back(new Piece(RD));
            Pieces->push_back(new Piece(LRU));
            int board_temp[] = {
                1, 0, 1, 1, 1, 0, 1, 0, 0, 0, 0, 
                1, 5, 1, 1, 1, 0, 0, 1, 0, 1, 1,
                1, 5, 1, 1, 1, 1, 0, 1, 1, 1, 1,
                1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 
                0, 5, 1, 0, 0, 1, 5, 1, 0, 0, 0,
                1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 123: {
            Pieces->push_back(new Piece(LD));
            Pieces->push_back(new Piece(LRUD));
            Pieces->push_back(new Piece(D));
            Pieces->push_back(new Piece(RU));
            Pieces->push_back(new Piece(U));
            int board_temp[] = {
                1, 1, 5, 0, 0, 0, 1, 0, 0, 0, 0,
                1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 5,
                1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 
                1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 5,
                5, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0,
                0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 5
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 124: {
            Pieces->push_back(new Piece(LU));
            Pieces->push_back(new Piece(D));
            Pieces->push_back(new Piece(R));
            Pieces->push_back(new Piece(RUD));
            Pieces->push_back(new Piece(L));
            int board_temp[] = {
                5, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 0, 
                0, 1, 0, 1, 1, 0, 1, 1, 0, 1, 5,
                5, 0, 0, 0, 1, 0, 1, 0, 1, 1, 5,
                0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 
                1, 1, 0, 1, 1, 0, 1, 5, 0, 0, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 125: {
            Pieces->push_back(new Piece(RUD));
            Pieces->push_back(new Piece(RU));
            Pieces->push_back(new Piece(U));
            Pieces->push_back(new Piece(R));
            Pieces->push_back(new Piece(L));
            int board_temp[] = {
                1, 1, 0, 0, 0, 0, 0, 0, 1, 1, 5,
                0, 5, 0, 0, 1, 0, 0, 0, 0, 0, 0,
                1, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 
                1, 1, 0, 0, 1, 1, 0, 1, 0, 0, 5,
                1, 0, 0, 0, 1, 1, 0, 1, 1, 1, 1, 
                1, 5, 0, 0, 0, 1, 0, 0, 0, 0, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 126: {
            Pieces->push_back(new Piece(RD));
            Pieces->push_back(new Piece(RU));
            Pieces->push_back(new Piece(LU));
            Pieces->push_back(new Piece(D));
            Pieces->push_back(new Piece(R));
            int board_temp[] = {
                1, 0, 0, 0, 5, 0, 0, 1, 1, 0, 5,
                1, 0, 0, 0, 1, 1, 1, 1, 1, 0, 1, 
                1, 1, 0, 1, 1, 1, 1, 1, 1, 0, 0, 
                0, 5, 0, 0, 1, 5, 0, 0, 1, 1, 0,
                1, 0, 0, 0, 1, 0, 0, 0, 5, 0, 0,
                1, 0, 1, 0, 1, 1, 1, 0, 0, 1, 0
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        case 127: {
            Pieces->push_back(new Piece(R));
            Pieces->push_back(new Piece(L));
            Pieces->push_back(new Piece(LRUD));
            Pieces->push_back(new Piece(U));
            Pieces->push_back(new Piece(RU));
            int board_temp[] = {
                0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 1,
                1, 0, 0, 1, 0, 0, 0, 0, 1, 5, 0,
                0, 0, 1, 0, 0, 1, 1, 1, 1, 0, 1, 
                5, 1, 1, 1, 1, 0, 1, 0, 0, 0, 1,
                0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 
                0, 5, 0, 0, 0, 0, 1, 0, 1, 1, 5
            };
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
        default:
        {
            Pieces->push_back(new Piece(R));
            int board_temp[] = {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
            for (int i = 0; i < Level::Height * Level::Width; i++)
            {
                Board[i] = board_temp[i];
            }
            break;
        }
    }
}