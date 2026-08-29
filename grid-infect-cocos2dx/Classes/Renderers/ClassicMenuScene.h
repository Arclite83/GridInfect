#ifndef __CLASSICMENU_SCENE_H__
#define __CLASSICMENU_SCENE_H__

#include "cocos2d.h"

class ClassicMenu : public cocos2d::CCLayer
{
public:
    // Here's a difference. Method 'init' in cocos2d-x returns bool, instead of returning 'id' in cocos2d-iphone
    virtual bool init();  

    // there's no 'id' in cpp, so we recommend returning the class instance pointer
    static cocos2d::CCScene* scene();
    
    // selector callbacks
    void homeButtonCallback(CCObject* pSender);
    void muteButtonCallback(CCObject* pSender);
    void goToLevelCallback(CCObject* pSender);
    void pageUpCallback(CCObject* pSender);
    void pageDownCallback(CCObject* pSender);
    
    // implement the "static node()" method manually
    CREATE_FUNC(ClassicMenu);
};

#endif // __CLASSICMENU_SCENE_H__
