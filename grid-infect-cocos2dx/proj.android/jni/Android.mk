LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)

LOCAL_MODULE := cocos2dcpp_shared

LOCAL_MODULE_FILENAME := libcocos2dcpp

LOCAL_SRC_FILES := hellocpp/main.cpp \
                   ../../Classes/GPG/GameCenter.cpp \
                   ../../Classes/AppDelegate.cpp \                   ../../Classes/Core/SaveData.cpp \                   ../../Classes/Core/Piece.cpp \                   ../../Classes/Core/Level.cpp \                   ../../Classes/Core/Repel.cpp \
                   ../../Classes/Core/Game.cpp \
                   ../../Classes/Events/EventHandler.cpp \
                   ../../Classes/LevelBuilder.cpp \
                   ../../Classes/Renderers/ClassicMenuScene.cpp \                   ../../Classes/Renderers/FreePlayMenuScene.cpp \                   ../../Classes/Renderers/LevelMenuScene.cpp \
                   ../../Classes/Renderers/MainMenuScene.cpp \
                   ../../Classes/Resources/SoundManager.cpp

LOCAL_C_INCLUDES := $(LOCAL_PATH)/../../Classes \
		    $(LOCAL_PATH)/../../Classes/Core \
		    $(LOCAL_PATH)/../../Classes/GPG \
		    $(LOCAL_PATH)/../../Classes/Events \
		    $(LOCAL_PATH)/../../Classes/Renderers \
		    $(LOCAL_PATH)/../../Classes/Resources

LOCAL_WHOLE_STATIC_LIBRARIES += cocos2dx_static
LOCAL_WHOLE_STATIC_LIBRARIES += cocosdenshion_static
LOCAL_WHOLE_STATIC_LIBRARIES += box2d_static
LOCAL_WHOLE_STATIC_LIBRARIES += chipmunk_static
LOCAL_WHOLE_STATIC_LIBRARIES += cocos_extension_static

include $(BUILD_SHARED_LIBRARY)

$(call import-module,cocos2dx)
$(call import-module,cocos2dx/platform/third_party/android/prebuilt/libcurl)
$(call import-module,CocosDenshion/android)
$(call import-module,external/Box2D)
$(call import-module,external/chipmunk)
