#ifndef __MAINMENU_SCENE_H__
#define __MAINMENU_SCENE_H__

#include "cocos2d.h"

class MainMenu : public cocos2d::CCLayer
{
public:
    // Here's a difference. Method 'init' in cocos2d-x returns bool, instead of returning 'id' in cocos2d-iphone
    virtual bool init();  

    // there's no 'id' in cpp, so we recommend returning the class instance pointer
    static cocos2d::CCScene* scene();
    
    // selector callbacks
    void gPlayButtonCallback(CCObject* pSender);
    void achievementsButtonCallback(CCObject* pSender);
    void classicButtonCallback(CCObject* pSender);
    void freePlayButtonCallback(CCObject* pSender);
    void muteButtonCallback(CCObject* pSender);
    void infoButtonCallback(CCObject* pSender);
    void gPlayPromoYesButtonCallback(CCObject* pSender);
    void gPlayPromoNoButtonCallback(CCObject* pSender);
    
    // implement the "static node()" method manually
    CREATE_FUNC(MainMenu);
};

#endif // __MAINMENU_SCENE_H__
