using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

    public class UIUtils
    {
        public static void BtnPointerUpEvt(Button btn, UnityAction<BaseEventData> action, bool isAdd)
        {
            if (btn == null)
                return;

            if (isAdd)
            {
                EventTrigger trigger = btn.transform.GetOrAddComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerUp;
                entry.callback = new EventTrigger.TriggerEvent();
                entry.callback.AddListener(action);
                trigger.triggers.Add(entry);
            }
            else
            {
                EventTrigger trigger = btn.transform.GetOrAddComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerUp;
                entry.callback = new EventTrigger.TriggerEvent();
                entry.callback.RemoveListener(action);
                trigger.triggers.Add(entry);
            }
        }

        public static void BtnPointerDownEvt(Button btn, UnityAction<BaseEventData> action, bool isAdd)
        {
            if (btn == null)
                return;

            if (isAdd)
            {
                EventTrigger trigger = btn.transform.GetOrAddComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerDown;
                entry.callback = new EventTrigger.TriggerEvent();
                entry.callback.AddListener(action);
                trigger.triggers.Add(entry);
            }
            else
            {
                EventTrigger trigger = btn.transform.GetOrAddComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerDown;
                entry.callback = new EventTrigger.TriggerEvent();
                entry.callback.RemoveListener(action);
                trigger.triggers.Add(entry);
            }
        }

    }