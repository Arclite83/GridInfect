#ifndef __FREEPLAYMENU_SCENE_H__
#define __FREEPLAYMENU_SCENE_H__

#include "cocos2d.h"

class FreePlayMenu : public cocos2d::CCLayer
{
public:
    // Here's a difference. Method 'init' in cocos2d-x returns bool, instead of returning 'id' in cocos2d-iphone
    virtual bool init();  

    // there's no 'id' in cpp, so we recommend returning the class instance pointer
    static cocos2d::CCScene* scene();
    
    // selector callbacks
    void freePlay1Callback(CCObject* pSender);
    void freePlay2Callback(CCObject* pSender);
    void freePlay3Callback(CCObject* pSender);
    void freePlay4Callback(CCObject* pSender);
    void freePlay5Callback(CCObject* pSender);
    void leaderboard1Callback(CCObject* pSender);
    void leaderboard2Callback(CCObject* pSender);
    void leaderboard3Callback(CCObject* pSender);
    void leaderboard4Callback(CCObject* pSender);
    void leaderboard5Callback(CCObject* pSender);
    void homeButtonCallback(CCObject* pSender);
    void muteButtonCallback(CCObject* pSender);
    
    // implement the "static node()" method manually
    CREATE_FUNC(FreePlayMenu);
};

#endif // __FREEPLAYMENU_SCENE_H__
