#8digits Windows Phone API 1.0

8digits Windows Phone API'si, Windows Phone uygulamalarında kullanılmak üzere yazılmış C# classlarından oluşan bir kütüphane niteliğindedir. API içerisinde EightDigits *namespace*'ine ait classları bulunmaktadır.

8digits API, kendi sınıflarının yanında NewtonSoft'un [Json.NET](http://james.newtonking.com/pages/json-net.aspx) kütüphanesini kullanır. Bu kütüphaneyi, kullanacağınız projeniz içerisinde [NuGet](http://www.nuget.org/) üzerinden kolayca yükleyebilirsiniz. 

Json.NET kütüphanesinin NuGet sayfası: [nuget.org/packages/Newtonsoft.Json](http://www.nuget.org/packages/Newtonsoft.Json)

NuGet yardım sayfası: [docs.nuget.org/docs/start-here/overview](http://docs.nuget.org/docs/start-here/overview)

##API Hakkında

8digits API'yi oluşturan bölümlere bir göz atalım.

###Visit

Uygulama her açıldığında bir visit oluşturulur ve bütün işlemler bu visit üzerinden yürür ve bir session içerisinde gruplandırılır. API authentikasyonu da bu visit üzerinden yapılır.

Uygulama kapandığında visit de sonlanmalıdır.

Bir visit başlatmak için `Username`, `Password`, `TrackingCode` ve `URLPrefix` parametrelerine ihtiyaç vardır. Bu bilgileri 8digits profil ayarları bölümünde bulabilirsiniz.

####Visitor

Uygulama ilk kez açıldığında uygulamayı açan kullanıcıya bir `VisitorID` verilir ve bu kullanıcı uygulamanızı her kullandığında cihazın sabit diskinde tutulan identifier ile tespit edilir.

Kullanıcıyla ilgili bilgiler (kullanıcı badgeleri ve skoru gibi) indentifier üzerinden takip edilir. 8digits API, bu bilgiyi kendisi oluşturarak kontrol eder.

8digits API, cihazı kullanan kullanıcının badgelerine ve skoruna ulaşma imkanının yanında, kullanıcı skorunu dilediğiniz kadar artırma ve azaltma imkanı da sağlar.

####Hit

Yeni bir ekran görüntülemesi yapılacağında o ekran için bir hit başlatmak gereklidir. Aynı anda birden fazla ekran aktif olabilir, yani yeni bir hit başlatmak için halihazırda aktif olan hitleri sonlandırmak gerekmez. Hit sonlanana kadar aktif olarak değerlendirilir.

Her hit, yani ekran için `Title` ve `Path` parametrelerine ihtiyaç vardır. Bu bilgiler free text olup, tamamen sizin ekranları birbirinden kolayca ayırt etmenize olanak sağlamak amacıyla bulunmaktadır.

####Event

Bir ekrandaki herhangi bir düğmeye basma, touch gesture veya butona basma işlemi 8digits’e gönderilebilir. Bu sayede o ekranda olan biten herhangi bir işlemi track etme imkanınız olabilmektedir. Bu işlem için eventler kullanılmaktadır.

Yeni bir event gönderebilmek için bir adet key ve bir adet value’ya ihtiyacınız vardır. Bu key değerlerini unique tutarsanız yapılan işlemler birbiri ile karışmayacaktır. Örneğin bir düğmeye basıldığında ve ürün incelenmeye alındığında key olarak `ProductWatch` value olarak da ürünün sizin taraftaki product id sini `L5308073` gönderebilirsiniz. Bu sayede hangi ürünün kaçar defa incelendiğini saatlik, günlük ve overall görebilme imkanınız olacaktır.

Uygulamalarda bir event kuşkusuz ki bir ekranda gerçekleşecektir. Eğer ekran için daha önceden bir hit oluşturulduysa gönderilen event bu hit ile ilişkilendirilebilir. Hiçbir hit ile ilişkilendirilmemiş eventler de tercih edilebilir.

##Kullanım
8digits API'yi, EightDigits `namespace`'indeki nesneleriyle iletişim kurarak kullanmanız gerekmektedir. Bu sebeple API'yi kullandığınız dosyaların başına `using EightDigits;` ibaresini koymanız işlerinizi kolaylaştıracaktır. 

##Visit Oluşturma ve Sonlandırma
Uygulama açıldığı anda bir visit başlatmalı ve uygulama kapandığında bu visiti sonlandırmalısınız. Başlattığınız visiti sonlandırmazsanız 8digits sunucusu uzun süre işlem yapılmadığından sizin yerinize otomatik olarak bu visiti sonlandıracaktır. 

Tercihen, `App.xaml.cs` dosyanızın içerisindeki `Application_Launching` ve `Application_Activated` metotları içerisinde visit başlatma ve `Application_Deactivated` metodu içerisinde visit sonlandırma işlemlerini şu kodlarla yapabilirsiniz:

```
// Visit start
Visit.Current.Start("username", "password", "tracking-code", "url-prefix");
```

```
// Visit end
Visit.Current.End();
```

Eğer kullanıcı adı ve şifrenizi herhangi bir şekilde uygulamanızın içerisinde *hardcoded* olarak tutmak istemiyorsanız, *authentication* işlemini kendi serverlarınız üzerinde yapıp 8digits API'ye sadece size dönen *auth token*'ı vererek visit başlatabilirsiniz. Bunun için elinizdeki *auth token*'ı kullanarak `Visit.Current.Start("auth-token");` metodunu çağırabilirsiniz.

**Not:** 8digits API'nin yaptığı başarılı/başarısız işlemleri loglarda görmek isterseniz, visit başlatılmadan hemen önce `Visit.Current.Logging = true;` satırını ekleyin. Uygulamanın herhangi bir yerinde loglamayı sonlandırmak için `Visit.Current.Logging = false` metodunu kullanabilirsiniz.

Uygulama genelinde kullanacağınız `Visit` nesnesine `Visit.Current` ile ulaşabilirsiniz. Bu visit nesnesini `new Visit()` şeklinde oluşturmanıza gerek yoktur. 8digits API bu işlemi kendisi yapar.

##Hit Oluşturma ve Sonlandırma

8digits API, içerisinde bulundurduğu `PhoneApplicationPage` *class extension*'ı sayesinde her `PhoneApplicationPage` ve `PhoneApplicationPage` alt sınıf nesnesine ait hit nesnelerine sınıfın içerisinden `this.GetHit()` ile ulaşabilmeye imkan sağlar. Hit oluşturma ve sonlandırma işlemlerini, tavsiye edildiği şekilde `PhoneApplicationPage` nesneleri içerisinde yapacaksanız bu ve diğer *extension* metotlarını kullanabilirsiniz. Bu durumda sizin yerinize oluşturulan `Hit` nesneleri `Path` değişkeni olarak içerisinde oluşturulduğu `PhoneApplicationPage` nesnesinin class adını, `Title` değişkeni olarak da `this.Title`'ı atayacaktır. Yani içerisinde hit başlatıp bitirdiğiniz `PhoneApplicationPage` nesnelerinin `Title` değişkenlerini doldurmanız tavsiye edilir. `PhoneApplicationPage` ekranı gösterildiğinde çağırılan `OnNavigatedTo` metodunun içerisine şu kodu ekleyerek bu ekrana ait bir hit oluşturabilir ve başlatabilirsiniz:

```
// Optionally, you can set the Title and Path properties
// this.GetHit().Title = "hit-title";
// this.GetHit().Path = "hit-path";
this.StartHit();
```

Bu kullanımda hiçbir hit nesnesi oluşturmanız gerekmez. `PhoneApplicationPageHitExtender` sizin yerinize bu işlemi yapar. Tabi eğer bu işlemi bir `PhoneApplicationPage` nesnesi içerisinde yapmıyorsanız kodunuzun şu şekilde olması gerekmektedir:

```
Hit hit = new Hit("hit-title", "hit-path");
hit.Start();
```

Başlatmış olduğunuz bir hiti şu şekilde sonlandırabilirsiniz:
```
this.EndHit();
```

##Event Gönderme

Gönderdiğiniz eventler bir hit ile ilişkili olabildiği gibi, herhangi bir hite bağlı olmayan eventler de gönderebilirsiniz. Bir UIViewController içerisinden, bu `PhoneApplicationPage` ekranıyla ilişkili event göndermek için şu kodu yazmanız yeterlidir:

```
this.TriggerEvent("key", "value");
```

Eğer kendi oluşturduğunuz bir hit üzerinden event göndermek isterseniz şu şekilde yapabilirsiniz:

```
Event anEvent = new Event("key", "value", this.hit);
anEvent.Trigger();
```

Kullandığınız `Hit` nesnesinin start metodu çağırılmış fakat hit henüz serverdan cevap almamışsa eventiniz bu cevap gelene kadar bekletilir, olumlu cevap geldiği anda sunucuya gönderilir.

Herhangi bir hit ile ilişkili olmayan bir event göndermek isterseniz bu işlemi `Visit` classı üzerinden yapabilirsiniz:

```
Visit.Current.TriggerEvent("key", "value");
```

Eğer event herhangi bir hit ile ilişkili değilse sunucuya direkt olarak gönderilir.

###Visitor Bilgileri

8digits API, uygulamanızı kullanan her kullanıcıya tekil bir ID atar. Kullanıcınızın badgeleri ve skoru bu ID üzerinden takip edilir. Uygulamanın herhangi bir yerinden o anki kullanıcıya `Visitor.Current` ile ulaşabilirsiniz. Visitor bilgileri ancak bir visit başlattığınızda geçerli olur.

####Badge bilgileri

Uygulamanızın o anki kullanıcısının badge bilgilerine `Visitor` nesnesinin `Badges` değişkeniyle ulaşabilirsiniz. Bu değişkenin değerinin `null` olması, badgelerin henüz yüklenmediği anlamına gelir. 8digits API, badgelerin sunucudan asenkron şekilde çekilmesini de sağlar:

```
this.Badges = Visitor.Current.Badges;

if (this.Badges == null) {
    Visitor.Current.OnBadgesLoaded += Visitor_OnBadgesLoaded(Visitor sender, VisitorEventArgs e);
	Visitor.Current.LoadBadges();
}

// ...

void Visitor_OnBadgesLoaded (Visitor sender, VisitorEventArgs e) {
	if (e.Error != null) {
		// Error is not nil, badge load failed, do something with the error
	}
	else {
		this.Badges = Visitor.Current.Badges;
		// Badges loaded successfully, update the UI
	}
}
```       

`LoadBadges()` metodunu bir kere çağırmanız sonucu `Visitor` nesnesinin `Badges` değişkeni güncellenecektir. Sonraki kullanımlarda badgelere direkt olarak `Visitor.Current.Badges` şeklinde erişebilirsiniz.

####Skor bilgileri

Uygulamanızın o anki kullanıcısının badge bilgilerine `Visitor` nesnesinin `Score` değişkeniyle ulaşabilirsiniz. Bu değişkenin değerinin `Visitor.ScoreNotLoaded` sabitine eşit olması, score bilgilerinin henüz yüklenmediği anlamına gelir. 8digits API, bu bilgilerin sunucudan asenkron şekilde çekilmesini de sağlar:

```
this.visitorScore = Visitor.Current.Score;

if (this.visitorScore == Visitor.ScoreNotLoaded) {
    Visitor.Current.OnScoreLoaded += Visitor_OnScoreLoaded(Visitor sender, VisitorEventArgs e);
	Visitor.Current.LoadScore();
}

// ...

void Visitor_OnScoreLoaded (Visitor sender, VisitorEventArgs e) {
	if (e.Error != null) {
		// Error is not nil, score load failed, do something with the error
	}
	else {
		this.visitorScore = Visitor.Current.Score;
		// Score loaded successfully, update the UI
	}
}
```       

8digits API bunun yanında, uygulamanızın o anki kullanıcısının skorunu yükseltmenize ya da düşürmenize de olanak sağlar:

```
Visitor.Current.OnScoreIncreased += Visitor_OnScoreIncreased(Visitor sender, VisitorEventArgs e);
Visitor.Current.IncreaseScore(42);
```

```
Visitor.Current.OnScoreDecreased += Visitor_OnScoreDecreased(Visitor sender, VisitorEventArgs e);
Visitor.Current.DecreaseScore(42);
```

EDVisitor nesnesinin `LoadScore()`, `IncreaseScore(int)` ya da `DecreaseScore(int)` metotlarından biri çağırıldığında score değişkeni de güncellenir. Bu evreden sonra kullanıcı skoruna `Visitor.Current.Score` şeklinde ulaşabilirsiniz.

####Visitor attribute bilgileri

Uygulamanızın o anki kullanıcısının *attribute* bilgilerine `Visitor` nesnesinin `VisitorAttributes` değişkeniyle ulaşabilirsiniz. 8digits API, asenkron şekilde visitor attribute değerleri atamanızı sağlar:

```
Visitor.OnAttributeSet += Visitor_OnAttributeSet;
Visitor.Current.SetAttribute("attribute-key", "attribute-value");

// ...

void Visitor_OnAttributeSet (Visitor sender, VisitorEventArgs e) {
	if (e.Error != null) {
		// Error is not nil, attribute set failed, do something with the error
	}
	else {
		// Attribute set successfully
	}
}
```       

`SetAttribute(String, String)` metodunu her çağırdığınızda `Visitor` nesnesinin `VisitorAttributes` değişkeni güncellenecektir.
