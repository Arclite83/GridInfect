#include "ClassicMenuScene.h"
#include "FreePlayMenuScene.h"
#include "GPGSManager.h"
#include "MainMenuScene.h"
#include "SaveData.h"
#include "SoundManager.h"

USING_NS_CC;

CCScene* MainMenu::scene()
{
    // 'scene' is an autorelease object
    CCScene *scene = CCScene::create();
    
    // 'layer' is an autorelease object
    MainMenu *layer = MainMenu::create();

    // add layer as a child to scene
    scene->addChild(layer);

    // return the scene
    return scene;
}

// on "init" you need to initialize your instance
bool MainMenu::init()
{
    //////////////////////////////
    // 1. super init first
    if ( !CCLayer::init() )
    {
        return false;
    }
    
    CCSize visibleSize = CCDirector::sharedDirector()->getVisibleSize();
    CCPoint origin = CCDirector::sharedDirector()->getVisibleOrigin();

    /////////////////////////////
    // 3. add your code below...

    CCSprite* backgroundSprite = CCSprite::create("background.png");
    backgroundSprite->setPosition(ccp(visibleSize.width/2 + origin.x, visibleSize.height/2 + origin.y));
    backgroundSprite->setScaleX(visibleSize.width/backgroundSprite->getContentSize().width);
    backgroundSprite->setScaleY(visibleSize.height/backgroundSprite->getContentSize().height);
    this->addChild(backgroundSprite, 0);
    
    CCSprite* logoSprite = CCSprite::create("logo.png");
    logoSprite->setPosition(ccp(visibleSize.width/2 + origin.x, origin.y + (visibleSize.height * 0.76f)));
    logoSprite->setScale(visibleSize.height/logoSprite->getContentSize().height * 0.21f);
    this->addChild(logoSprite, 0);
    
    bool isSignedIn = GPGSManager::IsSignedIn();
    
    CCMenuItemImage *gPlusButton = CCMenuItemImage::create("btn_g+.png",
                                                           "btn_g+_pressed.png",
                                                           this,
                                                           menu_selector(MainMenu::gPlayButtonCallback));
    gPlusButton->setScale(visibleSize.height/gPlusButton->getContentSize().height * 0.11f);
    gPlusButton->setPosition(ccp(origin.x
                                 + visibleSize.width * 0.98f
                                 - gPlusButton->getContentSize().width/2 * gPlusButton->getScaleX(),
                                 origin.y
                                 + visibleSize.height * 0.02f
                                 + gPlusButton->getContentSize().height/2 * gPlusButton->getScaleY()));
    if (isSignedIn)
    {
        gPlusButton->setNormalImage(CCSprite::create("btn_g+_pressed.png"));
        gPlusButton->setTag(1);
    }
    else
    {
        gPlusButton->setTag(0);
    }
    CCMenu* pMenuGPlus = CCMenu::create(gPlusButton, NULL);
    pMenuGPlus->setPosition(CCPointZero);
    pMenuGPlus->setTag(100);
    this->addChild(pMenuGPlus, 1);
    
    CCMenuItemImage *achievementsButton = CCMenuItemImage::create("btn_achievements_framed.png",
                                                                  "btn_achievements_framed_pressed.png",
                                                                  this,
                                                                  menu_selector(MainMenu::achievementsButtonCallback));
    achievementsButton->setScale(visibleSize.height/achievementsButton->getContentSize().height * 0.11f);
    achievementsButton->setPosition(ccp(origin.x
                                        + visibleSize.width * 0.98f
                                        - gPlusButton->getContentSize().width/2 * gPlusButton->getScaleX(),
                                        origin.y
                                        + visibleSize.height * 0.04f
                                        + achievementsButton->getContentSize().height/2 * achievementsButton->getScaleY()
                                        + gPlusButton->getContentSize().height * gPlusButton->getScaleY()));
    CCMenu* pMenuAchievements = CCMenu::create(achievementsButton, NULL);
    pMenuAchievements->setPosition(CCPointZero);
    this->addChild(pMenuAchievements, 1);

    CCMenuItemImage *classicButton = CCMenuItemImage::create("popup_bg.png",
                                                             "popup_bg_pressed.png",
                                                             this,
                                                             menu_selector(MainMenu::classicButtonCallback));
    classicButton->setScale(visibleSize.height/classicButton->getContentSize().height * 0.33f);
    
    float classicButtonX = origin.x
            + visibleSize.width * 0.49f
            - classicButton->getContentSize().width * 0.5f * classicButton->getScale();
    float classicButtonY = origin.y
            + visibleSize.height * 0.5f
            - classicButton->getContentSize().height * 0.5f * classicButton->getScale();
    
	classicButton->setPosition(ccp(classicButtonX, classicButtonY));
    CCMenu* pMenuClassic = CCMenu::create(classicButton, NULL);
    pMenuClassic->setPosition(CCPointZero);
    this->addChild(pMenuClassic, 1);
    
    CCLabelTTF* classicButtonText = CCLabelTTF::create("CLASSIC MODE\n \n128 LEVELS\nOF INCREASING\nDIFFICULTY",
                                                       "fonts/Overhaul.ttf",
                                          visibleSize.height * 0.044f);
    classicButtonText->cocos2d::CCNodeRGBA::setColor(ccWHITE);
    classicButtonText->setPosition(ccp(classicButtonX, classicButtonY));
    this->addChild(classicButtonText, 1);
    
    CCMenuItemImage *freePlayButton = CCMenuItemImage::create("popup_bg.png",
                                                              "popup_bg_pressed.png",
                                                              this,
                                                              menu_selector(MainMenu::freePlayButtonCallback));
    freePlayButton->setScale(visibleSize.height/freePlayButton->getContentSize().height * 0.33f);
    
    float freePlayButtonX = origin.x
            + visibleSize.width * 0.51f
            + freePlayButton->getContentSize().width * 0.5f * freePlayButton->getScale();
    float freePlayButtonY = origin.y
            + visibleSize.height * 0.5f
            - freePlayButton->getContentSize().height * 0.5f * freePlayButton->getScale();
    
	freePlayButton->setPosition(ccp(freePlayButtonX, freePlayButtonY));
    CCMenu* pMenuFreePlay = CCMenu::create(freePlayButton, NULL);
    pMenuFreePlay->setPosition(CCPointZero);
    this->addChild(pMenuFreePlay, 1);
    
    CCLabelTTF* freePlayButtonText = CCLabelTTF::create("FREE PLAY\n \nSOLVE 5 LEVELS,\nCOMPETE FOR THE\nFASTEST TIMES",
                                                        "fonts/Overhaul.ttf",
                                                        visibleSize.height * 0.044f);
    freePlayButtonText->cocos2d::CCNodeRGBA::setColor(ccWHITE);
    freePlayButtonText->setPosition(ccp(freePlayButtonX, freePlayButtonY));
    this->addChild(freePlayButtonText, 1);
    
    CCMenuItemImage *muteButton = CCMenuItemImage::create("btn_mute_on.png",
                                                          "btn_mute_on_pressed.png",
                                                          this,
                                                          menu_selector(MainMenu::muteButtonCallback));
    
    
    
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
    
    CCMenuItemImage *infoButton = CCMenuItemImage::create("btn_info_framed.png",
                                                          "btn_info_framed_pressed.png",
                                                          this,
                                                          menu_selector(MainMenu::infoButtonCallback));
    infoButton->setScale(visibleSize.height/infoButton->getContentSize().height * 0.11f);
    infoButton->setPosition(ccp(origin.x
                                + visibleSize.width * 0.98f
                                - gPlusButton->getContentSize().width/2 * gPlusButton->getScaleX(),
                                origin.y
                                + visibleSize.height * 0.06f
                                + infoButton->getContentSize().height/2 * infoButton->getScaleY()
                                + gPlusButton->getContentSize().height * gPlusButton->getScaleY()
                                + achievementsButton->getContentSize().height * achievementsButton->getScaleY()));
    CCMenu* pMenuInfo = CCMenu::create(infoButton, NULL);
    pMenuInfo->setPosition(CCPointZero);
    pMenuInfo->setTag(1000);
    this->addChild(pMenuInfo, 1);
    
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
    
    CCSprite* infoBackgroundSprite = CCSprite::create("popup_bg.png");
    infoBackgroundSprite->setScale(visibleSize.height/infoBackgroundSprite->getContentSize().height* 0.75f);
    infoBackgroundSprite->setPosition(ccp(origin.x
                                          + visibleSize.width * 0.5f,
                                          origin.y
                                          + visibleSize.height * 1.5f));
    infoBackgroundSprite->setTag(3000);
    this->addChild(infoBackgroundSprite, 2);
    
    const char *infoText = "Bloodhound Studios\nwww.bloodhoundstudios.com\n \nBased in Unionville, CT\nCopyright 2014\n \nCreated By: Christopher Mahar";
    CCLabelTTF* infoPanelText = CCLabelTTF::create(infoText, "fonts/Overhaul.ttf",
                                                   visibleSize.height * 0.044f);
    infoPanelText->setPosition(ccp(origin.x
                                   + visibleSize.width * 0.5f,
                                   origin.y
                                   + visibleSize.height * 1.5f));
    infoPanelText->setTag(3001);
    this->addChild(infoPanelText, 2);
    
    if (!SaveData::getInstance()->GetSkipGPlayPromo()&& !isSignedIn)
    {
        CCSprite* gPlayPromoBgSprite = CCSprite::create("gplay_promo_bg.png");
        gPlayPromoBgSprite->setPosition(ccp(origin.x
                                            + visibleSize.width * 0.5f,
                                            origin.y
                                            + visibleSize.height * 0.33f
                                            - visibleSize.height));
        gPlayPromoBgSprite->setScale(visibleSize.height/gPlayPromoBgSprite->getContentSize().height * 0.62f);
        gPlayPromoBgSprite->setTag(2000);
        this->addChild(gPlayPromoBgSprite, 2);
        
        CCMenuItemImage *gPlayPromoYesButton = CCMenuItemImage::create("Red-signin_Long_base.png",
                                                                       "Red-signin_Long_press.png",
                                                                       this,
                                                                       menu_selector(MainMenu::gPlayPromoYesButtonCallback));
        gPlayPromoYesButton->setScale(visibleSize.height/gPlayPromoYesButton->getContentSize().height * 0.09f);
        gPlayPromoYesButton->setPosition(ccp(origin.x
                                             + visibleSize.width * 0.5f,
                                             origin.y
                                             + visibleSize.height * 0.18f
                                             - visibleSize.height));
        gPlayPromoYesButton->setTag(2001);
        CCMenu* pMenuYesButton = CCMenu::create(gPlayPromoYesButton, NULL);
        pMenuYesButton->setPosition(CCPointZero);
        pMenuYesButton->setTag(2001);
        this->addChild(pMenuYesButton, 2);
        
        CCMenuItemImage *gPlayPromoNoButton = CCMenuItemImage::create("btn_no_thanks.png",
                                                                      "btn_no_thanks.png",
                                                                      this,
                                                                      menu_selector(MainMenu::gPlayPromoNoButtonCallback));
        gPlayPromoNoButton->setScale(visibleSize.height/gPlayPromoNoButton->getContentSize().height * 0.09f);
        gPlayPromoNoButton->setPosition(ccp(origin.x
                                            + visibleSize.width * 0.5f,
                                            origin.y
                                            + visibleSize.height * 0.08f
                                            - visibleSize.height));
        gPlayPromoNoButton->setTag(2002);
        CCMenu* pMenuNoButton = CCMenu::create(gPlayPromoNoButton, NULL);
        pMenuNoButton->setPosition(CCPointZero);
        pMenuNoButton->setTag(2002);
        this->addChild(pMenuNoButton, 2);
        
        float duration = 0.4f;
        CCFiniteTimeAction* actionMove1 = CCMoveTo::create(duration,
                                                           ccp(gPlayPromoBgSprite->getPositionX(),
                                                               gPlayPromoBgSprite->getPositionY() + visibleSize.height));
        gPlayPromoBgSprite->runAction(actionMove1);
        
        CCFiniteTimeAction* actionMove2 = CCMoveTo::create(duration,
                                                           ccp(gPlayPromoYesButton->getPositionX(),
                                                               gPlayPromoYesButton->getPositionY() + visibleSize.height));
        gPlayPromoYesButton->runAction(actionMove2);
        
        CCFiniteTimeAction* actionMove3 = CCMoveTo::create(duration,
                                                           ccp(gPlayPromoNoButton->getPositionX(),
                                                               gPlayPromoNoButton->getPositionY() + visibleSize.height));
        gPlayPromoNoButton->runAction(actionMove3);
    }
    
    SoundManager::getInstance()->playBgMusic();
    
    return true;
}

void MainMenu::gPlayPromoYesButtonCallback(CCObject* pSender)
{
    CCSize visibleSize = CCDirector::sharedDirector()->getVisibleSize();
    
    CCSprite* gPlayPromoBgSprite = (CCSprite*)this->getChildByTag(2000);
    CCMenuItemImage *gPlayPromoYesButton = (CCMenuItemImage *)this->getChildByTag(2001)->getChildByTag(2001);
    CCMenuItemImage *gPlayPromoNoButton = (CCMenuItemImage *)this->getChildByTag(2002)->getChildByTag(2002);

    float duration = 0.4f;
    CCFiniteTimeAction* actionMove1 = CCMoveTo::create(duration,
                                                      ccp(gPlayPromoBgSprite->getPositionX(),
                                                          gPlayPromoBgSprite->getPositionY() - visibleSize.height));
    gPlayPromoBgSprite->runAction(actionMove1);
    
    CCFiniteTimeAction* actionMove2 = CCMoveTo::create(duration,
                                                       ccp(gPlayPromoYesButton->getPositionX(),
                                                           gPlayPromoYesButton->getPositionY() - visibleSize.height));
    gPlayPromoYesButton->runAction(actionMove2);
    
    CCFiniteTimeAction* actionMove3 = CCMoveTo::create(duration,
                                                       ccp(gPlayPromoNoButton->getPositionX(),
                                                           gPlayPromoNoButton->getPositionY() - visibleSize.height));
    gPlayPromoNoButton->runAction(actionMove3);
    
    SaveData::getInstance()->SetSkipGPlayPromo(true);
    
    GPGSManager::BeginUserInitiatedSignIn();
}

void MainMenu::gPlayPromoNoButtonCallback(CCObject* pSender)
{
    CCSize visibleSize = CCDirector::sharedDirector()->getVisibleSize();
    
    CCSprite* gPlayPromoBgSprite = (CCSprite*)this->getChildByTag(2000);
    CCMenuItemImage *gPlayPromoYesButton = (CCMenuItemImage *)this->getChildByTag(2001)->getChildByTag(2001);
    CCMenuItemImage *gPlayPromoNoButton = (CCMenuItemImage *)this->getChildByTag(2002)->getChildByTag(2002);
    
    float duration = 0.4f;
    CCFiniteTimeAction* actionMove1 = CCMoveTo::create(duration,
                                                       ccp(gPlayPromoBgSprite->getPositionX(),
                                                           gPlayPromoBgSprite->getPositionY() - visibleSize.height));
    gPlayPromoBgSprite->runAction(actionMove1);
    
    CCFiniteTimeAction* actionMove2 = CCMoveTo::create(duration,
                                                       ccp(gPlayPromoYesButton->getPositionX(),
                                                           gPlayPromoYesButton->getPositionY() - visibleSize.height));
    gPlayPromoYesButton->runAction(actionMove2);
    
    CCFiniteTimeAction* actionMove3 = CCMoveTo::create(duration,
                                                       ccp(gPlayPromoNoButton->getPositionX(),
                                                           gPlayPromoNoButton->getPositionY() - visibleSize.height));
    gPlayPromoNoButton->runAction(actionMove3);
    
    SaveData::getInstance()->SetSkipGPlayPromo(true);
}

void MainMenu::gPlayButtonCallback(CCObject* pSender)
{
    CCMenuItemImage *gPlusButton = (CCMenuItemImage *)this->getChildByTag(100)->getChildren()->objectAtIndex(0);
    if (GPGSManager::IsSignedIn())
    {
        GPGSManager::BeginUserInitiatedSignIn();
        gPlusButton->setNormalImage(CCSprite::create("btn_g+_pressed.png"));
        gPlusButton->setTag(1);
    }
    else
    {
        GPGSManager::SignOut();
        gPlusButton->setNormalImage(CCSprite::create("btn_g+.png"));
        gPlusButton->setTag(0);
    }
}

void MainMenu::achievementsButtonCallback(CCObject* pSender)
{
    GPGSManager::ShowAchievements();
}

void MainMenu::classicButtonCallback(CCObject* pSender)
{
    CCScene *pScene = ClassicMenu::scene();
    CCDirector::sharedDirector()->replaceScene(CCTransitionFade::create( 0.5f, pScene));
}

void MainMenu::freePlayButtonCallback(CCObject* pSender)
{
    CCScene *pScene = FreePlayMenu::scene();
    CCDirector::sharedDirector()->replaceScene(CCTransitionFade::create( 0.5f, pScene));
}

void MainMenu::infoButtonCallback(CCObject* pSender)
{
    CCSize visibleSize = CCDirector::sharedDirector()->getVisibleSize();
    
    CCSprite* infoBgSprite = (CCSprite*)this->getChildByTag(3000);
    CCLabelTTF* infoBgText = (CCLabelTTF*)this->getChildByTag(3001);
    
    float duration = 0.3f;
    CCFiniteTimeAction* actionMove1;
    CCFiniteTimeAction* actionMove2;
    
    if (infoBgSprite->getPositionY() > visibleSize.height) {
    
        actionMove1 = CCMoveTo::create(duration,
                                       ccp(infoBgSprite->getPositionX(),
                                           visibleSize.height/2));
        actionMove2 = CCMoveTo::create(duration,
                                       ccp(infoBgText->getPositionX(),
                                           visibleSize.height/2));
    }
    else
    {
        
        actionMove1 = CCMoveTo::create(duration,
                                       ccp(infoBgSprite->getPositionX(),
                                           visibleSize.height*1.5));
        actionMove2 = CCMoveTo::create(duration,
                                       ccp(infoBgText->getPositionX(),
                                           visibleSize.height*1.5));
    }
    
    infoBgSprite->stopAllActions();
    infoBgText->stopAllActions();
    infoBgSprite->runAction(actionMove1);
    infoBgText->runAction(actionMove2);
}

void MainMenu::muteButtonCallback(CCObject* pSender)
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
