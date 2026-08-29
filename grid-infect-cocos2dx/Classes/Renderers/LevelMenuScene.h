#ifndef __LEVELMENU_SCENE_H__
#define __LEVELMENU_SCENE_H__

#include "cocos2d.h"
#include "IOnChangeBoardIndex.h"
#include "IOnLevelSolved.h"
#include "IOnUnbindPieces.h"

class LevelMenu : public cocos2d::CCLayer, IOnChangeBoardIndex, IOnLevelSolved, IOnUnbindPieces
{
public:
    // Here's a difference. Method 'init' in cocos2d-x returns bool, instead of returning 'id' in cocos2d-iphone
    virtual bool init();  

    // there's no 'id' in cpp, so we recommend returning the class instance pointer
    static cocos2d::CCScene* scene();
    
    // selector callbacks
    void beginButtonCallback(CCObject* pSender);
    void muteButtonCallback(CCObject* pSender);
    void menuCallback(CCObject* pSender);
    void replayCallback(CCObject* pSender);
    void nextCallback(CCObject* pSender);
    void delayFinished(CCNode* sender);
    
    void beginLevel();
    void bindLevel();
    void UnbindPiece(int i);
    std::string convertInt(int number);
    long millisecondNow();
    void updateFreePlayDisplay();
    
    
    // implement the "static node()" method manually
    CREATE_FUNC(LevelMenu);
    virtual void registerWithTouchDispatcher();
    virtual bool ccTouchBegan(cocos2d::CCTouch* touch, cocos2d::CCEvent* event);
    virtual void ccTouchEnded(cocos2d::CCTouch* touch, cocos2d::CCEvent* event);
    virtual void ccTouchCancelled(cocos2d::CCTouch* touch, cocos2d::CCEvent* event);
    virtual void ccTouchMoved(cocos2d::CCTouch* touch, cocos2d::CCEvent* event);
};

#endif // __LEVELMENU_SCENE_H__
