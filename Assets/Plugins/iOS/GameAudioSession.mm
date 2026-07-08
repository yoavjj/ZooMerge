#import <AVFoundation/AVFoundation.h>

extern "C"
{
    void SetGameAudioPlaybackMode()
    {
        AVAudioSession *session =
            [AVAudioSession sharedInstance];

        NSError *error = nil;

        [session setCategory:AVAudioSessionCategoryPlayback
                       error:&error];

        if (error != nil)
        {
            NSLog(
                @"Failed to set audio session category: %@",
                error
            );

            return;
        }

        [session setActive:YES error:&error];

        if (error != nil)
        {
            NSLog(
                @"Failed to activate audio session: %@",
                error
            );
        }
    }
}