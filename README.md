# SpillAlerts
Sewage spill alerts, straight to your inbox!

This is a simple background worker that checks for recent sewage spill reports (currently set up for Severn Trent only) and sends email notifications to a list of configured recipients whenever new spill locations pop up.

## Todo
[] Include sewage spill start time
[] Only alert Warickshire Avon Spills
[] Add opt-out footnote to email
[] Don't resend emails on redeploy. Assume that unless the slill was in the last 5 mins, we've already sent thr alert