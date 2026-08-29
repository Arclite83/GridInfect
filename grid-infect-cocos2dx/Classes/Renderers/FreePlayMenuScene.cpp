#include "FreePlayMenuScene.h"
#include "Game.h"
#include "GPGSManager.h"
#include "LevelBuilder.h"
#include "LevelMenuScene.h"
#include "MainMenuScene.h"
#include "SaveData.h"
#include "SoundManager.h"

USING_NS_CC;

CCScene* FreePlayMenu::scene()
{
    // 'scene' is an autorelease object
    CCScene *scene = CCScene::create();
    
    // 'layer' is an autorelease object
    FreePlayMenu *layer = FreePlayMenu::create();

    // add layer as a child to scene
    scene->addChild(layer);

    // return the scene
    return scene;
}

// on "init" you need to initialize your instance
bool FreePlayMenu::init()
{
    //////////////////////////////
    // 1. super init first
    if ( !CCLayer::init() )
    {
        return false;
    }
    
    CCSize visibleSize = CCDirector::sharedDirector()->getVisibleSize();
    CCPoint origin = CCDirector::sharedDirector()->getVisibleOrigin();

    CCSprite* backgroundSprite = CCSprite::create("background.png");
    backgroundSprite->setPosition(ccp(visibleSize.width/2 + origin.x, visibleSize.height/2 + origin.y));
    backgroundSprite->setScaleX(visibleSize.width/backgroundSprite->getContentSize().width);
    backgroundSprite->setScaleY(visibleSize.height/backgroundSprite->getContentSize().height);
    this->addChild(backgroundSprite, 0);
    
    long stat1 = SaveData::getInstance()->GetBestTime(Beginner);
    long stat2 = SaveData::getInstance()->GetBestTime(Easy);
    long stat3 = SaveData::getInstance()->GetBestTime(Medium);
    long stat4 = SaveData::getInstance()->GetBestTime(Hard);
    long stat5 = SaveData::getInstance()->GetBestTime(Challenging);
    
    std::stringstream statString1;
    if (stat1 > 0)
    {
        int mins = (stat1 / (1000 * 60)) % 60;
        int secs = (stat1 / 1000) % 60;
        int millis = stat1 % 1000;
        if (mins > 0)
        {
            statString1 << mins;
            statString1 << ":";
        }
        if (secs < 10)
        {
            statString1 << "0";
        }
        statString1 << secs;
        statString1 << ":";
        if (millis < 100)
        {
            statString1 << "0";
        }
        if (millis < 10)
        {
            statString1 << "0";
        }
        statString1 << millis;
    }
    else
    {
        statString1 << "--:--:---";
    }
    
    std::stringstream statString2;
    if (stat2 > 0)
    {
        int mins = (stat2 / (1000 * 60)) % 60;
        int secs = (stat2 / 1000) % 60;
        int millis = stat2 % 1000;
        if (mins > 0)
        {
            statString2 << mins;
            statString2 << ":";
        }
        if (secs < 10)
        {
            statString2 << "0";
        }
        statString2 << secs;
        statString2 << ":";
        if (millis < 100)
        {
            statString2 << "0";
        }
        if (millis < 10)
        {
            statString2 << "0";
        }
        statString2 << millis;
    }
    else
    {
        statString2 << "--:--:---";
    }
    
    std::stringstream statString3;
    if (stat3 > 0)
    {
        int mins = (stat3 / (1000 * 60)) % 60;
        int secs = (stat3 / 1000) % 60;
        int millis = stat3 % 1000;
        if (mins > 0)
        {
            statString3 << mins;
            statString3 << ":";
        }
        if (secs < 10)
        {
            statString3 << "0";
        }
        statString3 << secs;
        statString3 << ":";
        if (millis < 100)
        {
            statString3 << "0";
        }
        if (millis < 10)
        {
            statString3 << "0";
        }
        statString3 << millis;
    }
    else
    {
        statString3 << "--:--:---";
    }
    
    std::stringstream statString4;
    if (stat4 > 0)
    {
        int mins = (stat4 / (1000 * 60)) % 60;
        int secs = (stat4 / 1000) % 60;
        int millis = stat4 % 1000;
        if (mins > 0)
        {
            statString4 << mins;
            statString4 << ":";
        }
        if (secs < 10)
        {
            statString4 << "0";
        }
        statString4 << secs;
        statString4 << ":";
        if (millis < 100)
        {
            statString4 << "0";
        }
        if (millis < 10)
        {
            statString4 << "0";
        }
        statString4 << millis;
    }
    else
    {
        statString4 << "--:--:---";
    }
    
    std::stringstream statString5;
    if (stat5 > 0)
    {
        int mins = (stat5 / (1000 * 60)) % 60;
        int secs = (stat5 / 1000) % 60;
        int millis = stat5 % 1000;
        if (mins > 0)
        {
            statString5 << mins;
            statString5 << ":";
        }
        if (secs < 10)
        {
            statString5 << "0";
        }
        statString5 << secs;
        statString5 << ":";
        if (millis < 100)
        {
            statString5 << "0";
        }
        if (millis < 10)
        {
            statString5 << "0";
        }
        statString5 << millis;
    }
    else
    {
        statString5 << "--:--:---";
    }
    
    CCMenuItemImage *freePlay1Button = CCMenuItemImage::create(
                                                          "popup_bg.png",
                                                          "popup_bg_pressed.png",
                                                          this,
                                                          menu_selector(FreePlayMenu::freePlay1Callback));
    freePlay1Button->setScale(visibleSize.height/freePlay1Button->getContentSize().height * 0.27f);
    
    float freePlay1X = origin.x
            + visibleSize.width * 0.50f
            - freePlay1Button->getContentSize().width * freePlay1Button->getScaleX() * 1.1f;
    float freePlay1Y = origin.y
            + visibleSize.height * 0.93f
            - freePlay1Button->getContentSize().height/2 * freePlay1Button->getScaleY();
    freePlay1Button->setPosition(ccp(freePlay1X, freePlay1Y));
    CCMenu* pMenuFreePlay1 = CCMenu::create(freePlay1Button, NULL);
    pMenuFreePlay1->setPosition(CCPointZero);
    this->addChild(pMenuFreePlay1, 1);
    
    CCMenuItemImage *freePlay2Button = CCMenuItemImage::create(
                                                               "popup_bg.png",
                                                               "popup_bg_pressed.png",
                                                               this,
                                                               menu_selector(FreePlayMenu::freePlay2Callback));
    freePlay2Button->setScale(visibleSize.height/freePlay2Button->getContentSize().height * 0.27f);
    
    float freePlay2X = origin.x
            + visibleSize.width * 0.50f;
    float freePlay2Y = origin.y
            + visibleSize.height * 0.93f
            - freePlay2Button->getContentSize().height/2 * freePlay2Button->getScaleY();
    freePlay2Button->setPosition(ccp(freePlay2X, freePlay2Y));
    CCMenu* pMenufreePlay2 = CCMenu::create(freePlay2Button, NULL);
    pMenufreePlay2->setPosition(CCPointZero);
    this->addChild(pMenufreePlay2, 1);
    
    CCMenuItemImage *freePlay3Button = CCMenuItemImage::create(
                                                               "popup_bg.png",
                                                               "popup_bg_pressed.png",
                                                               this,
                                                               menu_selector(FreePlayMenu::freePlay3Callback));
    freePlay3Button->setScale(visibleSize.height/freePlay3Button->getContentSize().height * 0.27f);
    float freePlay3X = origin.x
            + visibleSize.width * 0.50f
            + freePlay3Button->getContentSize().width * freePlay3Button->getScaleX() * 1.1f;
    float freePlay3Y = origin.y
            + visibleSize.height * 0.93f
            - freePlay3Button->getContentSize().height/2 * freePlay3Button->getScaleY();
    freePlay3Button->setPosition(ccp(freePlay3X, freePlay3Y));
    CCMenu* pMenufreePlay3 = CCMenu::create(freePlay3Button, NULL);
    pMenufreePlay3->setPosition(CCPointZero);
    this->addChild(pMenufreePlay3, 1);
    
    CCMenuItemImage *freePlay4Button = CCMenuItemImage::create(
                                                               "popup_bg.png",
                                                               "popup_bg_pressed.png",
                                                               this,
                                                               menu_selector(FreePlayMenu::freePlay4Callback));
    freePlay4Button->setScale(visibleSize.height/freePlay4Button->getContentSize().height * 0.27f);
    float freePlay4X = origin.x
            + visibleSize.width * 0.50f
            - freePlay4Button->getContentSize().width * freePlay4Button->getScaleX() * 0.55f;
    float freePlay4Y = origin.y
            + visibleSize.height * 0.49f
            - freePlay4Button->getContentSize().height/2 * freePlay4Button->getScaleY();
    freePlay4Button->setPosition(ccp(freePlay4X, freePlay4Y));
    CCMenu* pMenufreePlay4 = CCMenu::create(freePlay4Button, NULL);
    pMenufreePlay4->setPosition(CCPointZero);
    this->addChild(pMenufreePlay4, 1);
    
    CCMenuItemImage *freePlay5Button = CCMenuItemImage::create("popup_bg.png",
                                                               "popup_bg_pressed.png",
                                                               this,
                                                               menu_selector(FreePlayMenu::freePlay5Callback));
    freePlay5Button->setScale(visibleSize.height/freePlay5Button->getContentSize().height * 0.27f);
    float freePlay5X = origin.x
            + visibleSize.width * 0.50f
            + freePlay5Button->getContentSize().width * freePlay5Button->getScaleX() * 0.55f;
    float freePlay5Y = origin.y
            + visibleSize.height * 0.49f
            - freePlay5Button->getContentSize().height/2 * freePlay5Button->getScaleY();
    freePlay5Button->setPosition(ccp(freePlay5X, freePlay5Y));
    CCMenu* pMenufreePlay5 = CCMenu::create(freePlay5Button, NULL);
    pMenufreePlay5->setPosition(CCPointZero);
    this->addChild(pMenufreePlay5, 1);
    
    CCMenuItemImage *leaderboard1 = CCMenuItemImage::create("btn_leaderboards_framed.png",
                                                            "btn_leaderboards_framed_pressed.png",
                                                            this,
                                                            menu_selector(FreePlayMenu::leaderboard1Callback));
    leaderboard1->setScale(visibleSize.height/leaderboard1->getContentSize().height * 0.13f);
    leaderboard1->setPosition(ccp(freePlay1X,
                                  origin.y
                                  + visibleSize.height * 0.64f
                                  - leaderboard1->getContentSize().height/2 * leaderboard1->getScaleY()));
    CCMenu* pMenuleaderboard1 = CCMenu::create(leaderboard1, NULL);
    pMenuleaderboard1->setPosition(CCPointZero);
    this->addChild(pMenuleaderboard1, 1);
    
    CCMenuItemImage *leaderboard2 = CCMenuItemImage::create("btn_leaderboards_framed.png",
                                                            "btn_leaderboards_framed_pressed.png",
                                                            this,
                                                            menu_selector(FreePlayMenu::leaderboard2Callback));
    leaderboard2->setScale(visibleSize.height/leaderboard2->getContentSize().height * 0.13f);
    leaderboard2->setPosition(ccp(freePlay2X,
                                  origin.y
                                  + visibleSize.height * 0.64f
                                  - leaderboard2->getContentSize().height/2 * leaderboard2->getScaleY()));
    CCMenu* pMenuleaderboard2 = CCMenu::create(leaderboard2, NULL);
    pMenuleaderboard2->setPosition(CCPointZero);
    this->addChild(pMenuleaderboard2, 1);
    
    CCMenuItemImage *leaderboard3 = CCMenuItemImage::create("btn_leaderboards_framed.png",
                                                            "btn_leaderboards_framed_pressed.png",
                                                            this,
                                                            menu_selector(FreePlayMenu::leaderboard3Callback));
    leaderboard3->setScale(visibleSize.height/leaderboard3->getContentSize().height * 0.13f);
    leaderboard3->setPosition(ccp(freePlay3X,
                                  origin.y
                                  + visibleSize.height * 0.64f
                                  - leaderboard3->getContentSize().height/2 * leaderboard3->getScaleY()));
    CCMenu* pMenuleaderboard3 = CCMenu::create(leaderboard3, NULL);
    pMenuleaderboard3->setPosition(CCPointZero);
    this->addChild(pMenuleaderboard3, 1);
    
    CCMenuItemImage *leaderboard4 = CCMenuItemImage::create("btn_leaderboards_framed.png",
                                                            "btn_leaderboards_framed_pressed.png",
                                                            this,
                                                            menu_selector(FreePlayMenu::leaderboard4Callback));
    leaderboard4->setScale(visibleSize.height/leaderboard4->getContentSize().height * 0.13f);
    leaderboard4->setPosition(ccp(freePlay4X,
                                  origin.y
                                  + visibleSize.height * 0.20f
                                  - leaderboard4->getContentSize().height/2 * leaderboard4->getScaleY()));
    CCMenu* pMenuleaderboard4 = CCMenu::create(leaderboard4, NULL);
    pMenuleaderboard4->setPosition(CCPointZero);
    this->addChild(pMenuleaderboard4, 1);
    
    CCMenuItemImage *leaderboard5 = CCMenuItemImage::create("btn_leaderboards_framed.png",
                                                            "btn_leaderboards_framed_pressed.png",
                                                            this,
                                                            menu_selector(FreePlayMenu::leaderboard5Callback));
    leaderboard5->setScale(visibleSize.height/leaderboard5->getContentSize().height * 0.13f);
    leaderboard5->setPosition(ccp(freePlay5X,
                                  origin.y
                                  + visibleSize.height * 0.20f
                                  - leaderboard5->getContentSize().height/2 * leaderboard5->getScaleY()));
    CCMenu* pMenuleaderboard5 = CCMenu::create(leaderboard5, NULL);
    pMenuleaderboard5->setPosition(CCPointZero);
    this->addChild(pMenuleaderboard5, 1);
    
    std::stringstream statText1;
    statText1 << "BEGINNER\nBEST TIME:\n";
    statText1 << statString1.str();
    
    std::stringstream statText2;
    if (SaveData::getInstance()->GetFreePlayCount(Beginner) < 3)
    {
        freePlay2Button->setEnabled(false);
        
        statText2 << "PLAY\nBEGINNER\n";
        int count = 3 - SaveData::getInstance()->GetFreePlayCount(Beginner);
        statText2 << count << " MORE";
        if (count == 1)
        {
            statText2 << " TIME";
        }
        else
        {
            statText2 << " TIMES";
        }
    }
    else
    {
        statText2 << "EASY\nBEST TIME:\n";
        statText2 << statString2.str();

        GPGSManager::UnlockAchievement("CgkIyMfpr_gNEAIQDQ");
    }
    
    std::stringstream statText3;
    
    if (SaveData::getInstance()->GetFreePlayCount(Beginner) < 3)
    {
        freePlay3Button->setEnabled(false);
        
        statText3 << "MEDIUM\nLOCKED";
    }
    else if (SaveData::getInstance()->GetFreePlayCount(Easy) < 3)
    {
        freePlay3Button->setEnabled(false);
        
        statText3 << "PLAY EASY\n";
        int count = 3 - SaveData::getInstance()->GetFreePlayCount(Easy);
        statText3 << count << " MORE";
        if (count == 1)
        {
            statText3 << " TIME";
        }
        else
        {
            statText3 << " TIMES";
        }
    }
    else
    {
        statText3 << "MEDIUM\nBEST TIME:\n";
        statText3 << statString3.str();
        
        GPGSManager::UnlockAchievement("CgkIyMfpr_gNEAIQDg");
    }
    
    std::stringstream statText4;
    if (SaveData::getInstance()->GetFreePlayCount(Easy) < 3)
    {
        freePlay4Button->setEnabled(false);
        
        statText4 << "HARD\nLOCKED";
    }
    else if (SaveData::getInstance()->GetFreePlayCount(Medium) < 3)
    {
        freePlay4Button->setEnabled(false);
        
        statText4 << "PLAY MEDIUM\n";
        int count = 3 - SaveData::getInstance()->GetFreePlayCount(Medium);
        statText4 << count << " MORE";
        if (count == 1)
        {
            statText4 << " TIME";
        }
        else
        {
            statText4 << " TIMES";
        }
    }
    else
    {
        statText4 << "HARD\nBEST TIME:\n";
        statText4 << statString4.str();
        
        GPGSManager::UnlockAchievement("CgkIyMfpr_gNEAIQDw");
    }
    
    std::stringstream statText5;
    if (SaveData::getInstance()->GetFreePlayCount(Medium) < 3)
    {
        freePlay5Button->setEnabled(false);
        
        statText5 << "CHALLENGING\nLOCKED";
    }
    else if (SaveData::getInstance()->GetFreePlayCount(Hard) < 3)
    {
        freePlay5Button->setEnabled(false);
        
        statText5 << "PLAY HARD\n";
        int count = 3 - SaveData::getInstance()->GetFreePlayCount(Hard);
        statText5 << count << " MORE";
        if (count == 1)
        {
            statText5 << " TIME";
        }
        else
        {
            statText5 << " TIMES";
        }
    }
    else
    {
        statText5 << "CHALLENGING\nBEST TIME:\n";
        statText5 << statString5.str();
        
        GPGSManager::UnlockAchievement("CgkIyMfpr_gNEAIQEA");
    }
    

    CCLabelTTF* text1 = CCLabelTTF::create(statText1.str().c_str(),
                                          "fonts/Overhaul.ttf",
                                          visibleSize.height * 0.045f);
    text1->cocos2d::CCNodeRGBA::setColor(ccWHITE);
    text1->setPosition(ccp(freePlay1X,freePlay1Y));
    this->addChild(text1, 2);
    
    CCLabelTTF* text2 = CCLabelTTF::create(statText2.str().c_str(),
                                           "fonts/Overhaul.ttf",
                                           visibleSize.height * 0.045f);
    text2->cocos2d::CCNodeRGBA::setColor(ccWHITE);
    text2->setPosition(ccp(freePlay2X,freePlay2Y));
    this->addChild(text2, 2);
    
    CCLabelTTF* text3 = CCLabelTTF::create(statText3.str().c_str(),
                                           "fonts/Overhaul.ttf",
                                           visibleSize.height * 0.045f);
    text3->cocos2d::CCNodeRGBA::setColor(ccWHITE);
    text3->setPosition(ccp(freePlay3X,freePlay3Y));
    this->addChild(text3, 2);
    
    CCLabelTTF* text4 = CCLabelTTF::create(statText4.str().c_str(),
                                           "fonts/Overhaul.ttf",
                                           visibleSize.height * 0.045f);
    text4->cocos2d::CCNodeRGBA::setColor(ccWHITE);
    text4->setPosition(ccp(freePlay4X,freePlay4Y));
    this->addChild(text4, 2);
    
    CCLabelTTF* text5 = CCLabelTTF::create(statText5.str().c_str(),
                                           "fonts/Overhaul.ttf",
                                           visibleSize.height * 0.045f);
    text5->cocos2d::CCNodeRGBA::setColor(ccWHITE);
    text5->setPosition(ccp(freePlay5X,freePlay5Y));
    this->addChild(text5, 2);
    
    CCMenuItemImage *homeButton = CCMenuItemImage::create(
                                                          "btn_home_framed.png",
                                                          "btn_home_framed_pressed.png",
                                                          this,
                                                          menu_selector(FreePlayMenu::homeButtonCallback));
    homeButton->setScale(visibleSize.height/homeButton->getContentSize().height * 0.11f);
    homeButton->setPosition(ccp(origin.x
                                + visibleSize.width * 0.98f
                                - homeButton->getContentSize().width/2 * homeButton->getScaleX(),
                                origin.y
                                + visibleSize.height * 0.02f
                                + homeButton->getContentSize().height/2 * homeButton->getScaleY()));
    CCMenu* pMenuHome = CCMenu::create(homeButton, NULL);
    pMenuHome->setPosition(CCPointZero);
    this->addChild(pMenuHome, 1);
    
    CCMenuItemImage *muteButton = CCMenuItemImage::create(
                                                          "btn_mute_on.png",
                                                          "btn_mute_on_pressed.png",
                                                          this,
                                                          menu_selector(FreePlayMenu::muteButtonCallback));
    
	muteButton->setPosition(ccp(origin.x
                                + visibleSize.width
                                - muteButton->getContentSize().width/2 ,
                                origin.y
                                + muteButton->getContentSize().height/2));
    muteButton->setScale(visibleSize.height/muteButton->getContentSize().height * 0.11f);
    muteButton->setPosition(ccp(origin.x
                                + visibleSize.width * 0.02f
                                + muteButton->getContentSize().width/2 * muteButton->getScaleX(),
                                origin.y
                                + visibleSize.height * 0.02f
                                + muteButton->getContentSize().height/2 * muteButton->getScaleY()));
    muteButton->setTag(1000);
    CCMenu* pMenuMute = CCMenu::create(muteButton, NULL);
    pMenuMute->setPosition(CCPointZero);
    pMenuMute->setTag(1000);
    this->addChild(pMenuMute, 1);
    
    if (SoundManager::getInstance()->isMute())
    {
        muteButton->setNormalImage(CCSprite::create("btn_mute_off.png"));
        muteButton->setSelectedImage(CCSprite::create("btn_mute_off_pressed.png"));
    }
    else
    {
        muteButton->setNormalImage(CCSprite::create("btn_mute_on.png"));
        muteButton->setSelectedImage(CCSprite::create("btn_mute_on_pressed.png"));
    }
    
    return true;
}

void FreePlayMenu::freePlay1Callback(CCObject* pSender)
{
    std::vector<Level*>* newLevels = new std::vector<Level*>();
    for(int i = 0; i < 5; i++)
    {
        Level* level = LevelBuilder::generateLevel(Beginner);
        newLevels->push_back(level);
    }
    Game::getInstance()->setLevels(newLevels, Beginner);
    CCScene *pScene = LevelMenu::scene();
    CCDirector::sharedDirector()->replaceScene(CCTransitionFade::create(0.5f, pScene));
}

void FreePlayMenu::freePlay2Callback(CCObject* pSender)
{
    std::vector<Level*>* newLevels = new std::vector<Level*>();
    for(int i = 0; i < 5; i++)
    {
        newLevels->push_back(LevelBuilder::generateLevel(Easy));
    }
    Game::getInstance()->setLevels(newLevels, Easy);
    CCScene *pScene = LevelMenu::scene();
    CCDirector::sharedDirector()->replaceScene(CCTransitionFade::create(0.5f, pScene));
}

void FreePlayMenu::freePlay3Callback(CCObject* pSender)
{
    std::vector<Level*>* newLevels = new std::vector<Level*>();
    for(int i = 0; i < 5; i++)
    {
        newLevels->push_back(LevelBuilder::generateLevel(Medium));
    }
    Game::getInstance()->setLevels(newLevels, Medium);
    CCScene *pScene = LevelMenu::scene();
    CCDirector::sharedDirector()->replaceScene(CCTransitionFade::create(0.5f, pScene));
}

void FreePlayMenu::freePlay4Callback(CCObject* pSender)
{
    std::vector<Level*>* newLevels = new std::vector<Level*>();
    for(int i = 0; i < 5; i++)
    {
        newLevels->push_back(LevelBuilder::generateLevel(Hard));
    }
    Game::getInstance()->setLevels(newLevels, Hard);
    CCScene *pScene = LevelMenu::scene();
    CCDirector::sharedDirector()->replaceScene(CCTransitionFade::create(0.5f, pScene));
}

void FreePlayMenu::freePlay5Callback(CCObject* pSender)
{
    std::vector<Level*>* newLevels = new std::vector<Level*>();
    for(int i = 0; i < 5; i++)
    {
        newLevels->push_back(LevelBuilder::generateLevel(Challenging));
    }
    Game::getInstance()->setLevels(newLevels, Challenging);
    CCScene *pScene = LevelMenu::scene();
    CCDirector::sharedDirector()->replaceScene(CCTransitionFade::create(0.5f, pScene));
}

void FreePlayMenu::leaderboard1Callback(CCObject* pSender)
{
    GPGSManager::ShowLeaderboard("CgkIyMfpr_gNEAIQBw");
    
}

void FreePlayMenu::leaderboard2Callback(CCObject* pSender)
{
    GPGSManager::ShowLeaderboard("CgkIyMfpr_gNEAIQCA");
}

void FreePlayMenu::leaderboard3Callback(CCObject* pSender)
{
    GPGSManager::ShowLeaderboard("CgkIyMfpr_gNEAIQCQ");
}

void FreePlayMenu::leaderboard4Callback(CCObject* pSender)
{
    GPGSManager::ShowLeaderboard("CgkIyMfpr_gNEAIQCg");
}

void FreePlayMenu::leaderboard5Callback(CCObject* pSender)
{
    GPGSManager::ShowLeaderboard("CgkIyMfpr_gNEAIQCw");
}

void FreePlayMenu::homeButtonCallback(CCObject* pSender)
{
    CCScene *pScene = MainMenu::scene();
    CCDirector::sharedDirector()->replaceScene(CCTransitionFade::create(0.5f, pScene));
}

void FreePlayMenu::muteButtonCallback(CCObject* pSender)
{
    SoundManager::getInstance()->toggleMute();
    
    CCMenuItemImage* muteButton = (CCMenuItemImage*)this->getChildByTag(1000)->getChildByTag(1000);
    if (SoundManager::getInstance()->isMute())
    {
        muteButton->setNormalImage(CCSprite::create("btn_mute_off.png"));
        muteButton->setSelectedImage(CCSprite::create("btn_mute_off_pressed.png"));
    }
    else
    {
        muteButton->setNormalImage(CCSprite::create("btn_mute_on.png"));
        muteButton->setSelectedImage(CCSprite::create("btn_mute_on_pressed.png"));
    }
}