#pragma once
#include <atomic>
#include <future>
#include <memory>
#include "asyncop.h"
#include "ispxinterfaces.h"
#include "named_properties_impl.h"


namespace CARBON_IMPL_NAMESPACE() {


class CSpxRecognizer :
    public ISpxRecognizer,
    public ISpxRecognizerEvents,
    public ISpxNamedPropertiesImpl,
    public ISpxSessionFromRecognizer,
    public ISpxObjectWithSiteInitImpl<ISpxRecognizerSite>
{
public:

    CSpxRecognizer();
    virtual ~CSpxRecognizer();

    // --- ISpxObjectWithSiteInit
    void Init() override;
    void Term() override;

    // --- ISpxNamedProperties (overrides)
    void SetStringValue(const wchar_t* name, const wchar_t* value) override;

    // --- ISpxRecognizer

    bool IsEnabled() override;
    void Enable() override;
    void Disable() override;

    CSpxAsyncOp<std::shared_ptr<ISpxRecognitionResult>> RecognizeAsync() override;
    CSpxAsyncOp<void> StartContinuousRecognitionAsync() override;
    CSpxAsyncOp<void> StopContinuousRecognitionAsync() override;

    CSpxAsyncOp<void> StartKeywordRecognitionAsync(const wchar_t* keyword) override;
    CSpxAsyncOp<void> StopKeywordRecognitionAsync() override;

    // --- ISpxSessionFromRecognizer

    std::shared_ptr<ISpxSession> GetDefaultSession() override;

    // --- ISpxRecognizerEvents

    void FireSessionStarted(const std::wstring& sessionId) override;
    void FireSessionStopped(const std::wstring& sessionId) override;

    void FireSpeechStartDetected(const std::wstring& sessionId) override;
    void FireSpeechEndDetected(const std::wstring& sessionId) override;

    void FireResultEvent(const std::wstring& sessionId, std::shared_ptr<ISpxRecognitionResult> result) override;


protected:

    void EnsureDefaultSession();
    void TermDefaultSession();
    
    void OnIsEnabledChanged();

    std::shared_ptr<ISpxNamedProperties> GetParentProperties() override;


private:

    CSpxRecognizer(const CSpxRecognizer&) = delete;
    CSpxRecognizer(const CSpxRecognizer&&) = delete;

    CSpxRecognizer& operator=(const CSpxRecognizer&) = delete;

    std::shared_ptr<ISpxSession> m_defaultSession;
    std::atomic_bool m_fEnabled;
};


} // CARBON_IMPL_NAMESPACE
