//
//  SoundManager.cpp
//  GridInfect
//
//  Created by Christopher Mahar on 4/2/14.
//
//

#include "SoundManager.h"
#include "SimpleAudioEngine.h"
#include "SaveData.h"

SoundManager* SoundManager::_instance = NULL;
SoundManager* SoundManager::getInstance()
{
    if (!_instance)
    {
        _instance = new SoundManager;
    }
    return _instance;
}

SoundManager::SoundManager()
{
}

void SoundManager::setMuted(bool isMuted) {
    
    SaveData::getInstance()->SetMute(isMuted);
    
    if (isMuted) {
        stopBgMusic();
    } else {
        playBgMusic();
    }
}

bool SoundManager::isMute() {
    return SaveData::getInstance()->GetMute();
}

void SoundManager::toggleMute() {
    setMuted(!isMute());
}

void SoundManager::playBgMusic() {
    if (!isMute()) {
        CocosDenshion::SimpleAudioEngine::sharedEngine()->playBackgroundMusic("POL-pencil-maze-long.wav", true);
    }
}

void SoundManager::pauseBgMusic() {
    CocosDenshion::SimpleAudioEngine::sharedEngine()->pauseBackgroundMusic();
}

void SoundManager::resumeBgMusic() {
    if (!isMute()) {
        CocosDenshion::SimpleAudioEngine::sharedEngine()->resumeBackgroundMusic();
    }
}

void SoundManager::stopBgMusic() {
    CocosDenshion::SimpleAudioEngine::sharedEngine()->stopBackgroundMusic();
}

void SoundManager::playClickSound() {
    if (!isMute()) {
        CocosDenshion::SimpleAudioEngine::sharedEngine()->playEffect("click.wav");
    }
}