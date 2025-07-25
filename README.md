# SpillAlerts
**Sewage spill alerts, straight to your inbox!**

This is a simple background worker that checks for recent sewage spill reports on the Warwickshire Avon (currently set up for Severn Trent only) and sends email notifications to a list of configured recipients whenever new spill locations pop up.

## Todo
- [ ] Unit tests!
- [x] Include sewage spill start time  
- [x] Only alert Warwickshire Avon spills  
- [x] Add opt-out footnote to email  
- [x] Don't resend emails on redeploy — assume that unless the spill was in the last 5 minutes, we've already sent the alert
- [ ] Telegram Integration
- [ ] Automatically opt-user in from smartsurvey form
- [x] Grace period
- [ ] Pull in rain water data to see if it's a dry spill
- [x] Add the size/type of station
- [ ] Batch the alerts?
- [ ] Pull address back in

## Cool to have
- [ ] Styled emails
- [ ] Include map on email
