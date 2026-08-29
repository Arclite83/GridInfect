//
//  SoundManager.h
//  GridInfect
//
//  Created by Christopher Mahar on 4/2/14.
//
//

#ifndef __GridInfect__SoundManager__
#define __GridInfect__SoundManager__

#include <iostream>

class SoundManager
{
public:
    static SoundManager* getInstance();
    
    void setMuted(bool isMuted);
    bool isMute();
    void toggleMute();
    void playBgMusic();
    void pauseBgMusic();
    void resumeBgMusic();
    void stopBgMusic();
    void playClickSound();
private:
    SoundManager();
    SoundManager(SoundManager const&){};
    void operator=(SoundManager const&){};
    static SoundManager* _instance;
};

#endif /* defined(__GridInfect__SoundManager__) */
