//
//  SaveData.cpp
//  GridInfect
//
//  Created by Christopher Mahar on 4/7/14.
//
//

#include "SaveData.h"
#include "cocos2d.h"
#include "Level.h"

#include <iostream>
#include <fstream>
#include <string>
using namespace std;

SaveData* SaveData::_instance = NULL;
SaveData* SaveData::getInstance()
{
    if (!_instance)
    {
        _instance = new SaveData;
    }
    return _instance;
}

std::string fileName = "GridInfectSave.txt";
bool mute;
bool unlocked[Level::Count];
int freePlayBestTimes[5];
int freePlayCount[5];
bool skipGPlayPromo;

SaveData::SaveData()
{
    string path = cocos2d::CCFileUtils::sharedFileUtils()->getWritablePath();
    string fullFile = path.append(fileName);
    
    string line;
    ifstream myfile (fullFile.c_str());
    if (myfile.is_open())
    {
        while (getline(myfile, line))
        {
            size_t found=line.find("MUTE:");
            if (found!=string::npos)
            {
                string m = line.substr(found+5);
                int value = atoi(m.c_str());
                mute = (value == 1);
            }
            found=line.find("SKIPGPLAYPROMO:");
            if (found!=string::npos)
            {
                string m = line.substr(found+15);
                int value = atoi(m.c_str());
                skipGPlayPromo = (value == 1);
            }
            found=line.find("UNLOCKED:");
            if (found!=string::npos)
            {
                string a = line.substr(found+9);
                int value = atoi(a.c_str());
                unlocked[value] = true;
            }
            found=line.find("LEADERBOARD");
            if (found!=string::npos)
            {
                string i = line.substr(found+11, 1);
                string v = line.substr(found+13);
                int value_i = atoi(i.c_str());
                int value_v = atoi(v.c_str());
                freePlayBestTimes[value_i] = value_v;
            }
            found=line.find("FREEPLAYCOUNT");
            if (found!=string::npos)
            {
                string i = line.substr(found+13, 1);
                string v = line.substr(found+15);
                int value_i = atoi(i.c_str());
                int value_v = atoi(v.c_str());
                freePlayCount[value_i] = value_v;
            }
        }
    }
}

void SaveData::Save()
{
    string path = cocos2d::CCFileUtils::sharedFileUtils()->getWritablePath();
    string fullFile = path.append(fileName);
    
    ofstream myfile (fullFile.c_str());
    if (myfile.is_open())
    {
        myfile << "MUTE:" << (int)mute << endl;
        myfile << "SKIPGPLAYPROMO:" << (int)skipGPlayPromo << endl;
        for (int i = 0; i < Level::Count; i++)
        {
            if (unlocked[i])
            {
                myfile << "UNLOCKED:" << i << endl;
            }
        }
        for (int i = 0; i < 5; i++)
        {
            myfile << "LEADERBOARD" << i << ":" << freePlayBestTimes[i] << endl;
            myfile << "FREEPLAYCOUNT" << i << ":" << freePlayCount[i] << endl;
        }
        myfile.close();
    }
}

bool SaveData::IsUnlocked(int value)
{
    return unlocked[value];
}

void SaveData::Unlock(int value)
{
    unlocked[value] = true;
    Save();
}

int SaveData::GetBestTime(Difficulty difficulty)
{
    return freePlayBestTimes[difficulty];
}

void SaveData::SetBestTime(Difficulty difficulty, int value)
{
    freePlayBestTimes[difficulty] = value;
    Save();
}

void SaveData::IncrementFreePlayCount(Difficulty difficulty)
{
    freePlayCount[difficulty]++;
    Save();
}

int SaveData::GetFreePlayCount(Difficulty difficulty)
{
    return freePlayCount[difficulty];
}

bool SaveData::GetMute()
{
    return mute;
}

void SaveData::SetMute(bool value)
{
    mute = value;
    Save();
}

bool SaveData::GetSkipGPlayPromo()
{
    return skipGPlayPromo;
}

void SaveData::SetSkipGPlayPromo(bool value)
{
    skipGPlayPromo = value;
    Save();
}