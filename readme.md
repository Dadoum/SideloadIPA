# SideloadIPA
Sideload IPA is a Cydia-Impactor like tool.

## How it works
Apple uses two protocols in the authentication process:
 - Secure remote password protocol defined by RFC-5054
 - Anisette data exchange a.k.a. GrandSlam

The first protocol is implemented into Sideload IPA with SRP.NET library.
The second is not implemented yet.

But you can get further informations in the thread linked in Special thanks

After 3 requests on GSA, containing the SRP exchange and Anisette data, we have a token "X-Apple-GS-Token". (and "X-Apple-I-MD" shits)
These tokens permit us to use Apple's API like myacinfo before. 

## Special thanks
 - kabiroberai (Supercharge)
 - Matchstic (ReProvison)
 - ryleytestut (AltSign)
 - All guys from this [thread](https://github.com/horrorho/InflatableDonkey/issues/87)
