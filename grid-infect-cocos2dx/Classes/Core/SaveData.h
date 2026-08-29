//
//  SaveData.h
//  GridInfect
//
//  Created by Christopher Mahar on 4/7/14.
//
//

#ifndef __GridInfect__SaveData__
#define __GridInfect__SaveData__

#include <iostream>
#include "cocos2d.h"
#include "Enums.h"

class SaveData
{
public:
    static SaveData* getInstance();
    
    void Save();
    bool IsUnlocked(int value);
    void Unlock(int value);
    int GetBestTime(Difficulty difficulty);
    void SetBestTime(Difficulty difficulty, int value);
    int GetFreePlayCount(Difficulty difficulty);
    void IncrementFreePlayCount(Difficulty difficulty);
    bool GetMute();
    void SetMute(bool value);
    bool GetSkipGPlayPromo();
    void SetSkipGPlayPromo(bool value);
private:
    SaveData();
    SaveData(SaveData const&){};
    void operator=(SaveData const&){};
    static SaveData* _instance;
    
    void Load();
};

#endif /* defined(__GridInfect__SaveData__) */
