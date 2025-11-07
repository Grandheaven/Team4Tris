using UnityEngine;
using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using TMPro;
using UnityEngine.UI;

public class Page2_DB : MonoBehaviour
{
    [SerializeField] private string host = "deu.duraka.shop";
    [SerializeField] private string port = "4264";
    [SerializeField] private string sid = "xe";
    [SerializeField] private string userid = "TEAM4";
    [SerializeField] private string password = "Team4Tris";

    private string savetel;
    private string saveemail;

    static public bool run = false;
    static public bool delete = false;
    static public bool isDeleted = false;
    static public string dataname;
    static public string databirth;
    static public string datasex;
    static public string datatel;

    public DynamicTableController tableController;

    [Header("MemberInformation Object")]
    public GameObject memberinfo;
    public TMP_InputField username;
    public TMP_InputField birth;
    public TMP_InputField sex;
    public TMP_InputField tel;
    public TMP_InputField email;
    public RawImage photo;

    [Header("MemberModify Object")]
    public GameObject memberinfo2;
    public TMP_InputField username2;
    public TMP_InputField birth2;
    public TMP_InputField sex2;
    public TMP_InputField tel2;
    public TMP_InputField email2;
    public RawImage photo2;
    public GameObject background1;
    public GameObject background2;
    public GameObject background3;

    [Header("Pop-Up Object")]
    public GameObject successUI;
    public GameObject alertUI;
    public TMP_Text alertText;
    public GameObject failUI;
    public TMP_Text failText;
    public GameObject deleteUI;

    [Header("delete error")]
    public GameObject failDeleteUI;     // 🌟 삭제 실패 시 띄울 새로운 팝업

    void Update()
    {
        if (tableController == null)
        {
            tableController = FindObjectOfType<DynamicTableController>();
        }

        if (memberinfo != null && memberinfo.activeInHierarchy && run)
        {
            MemberInformation();
            run = false;
        }
        if (memberinfo2 != null && memberinfo2.activeInHierarchy && run)
        {
            MemberModify();
            run = false;
        }
        if (delete == true && run == true)
        {
            MemberDelete();
            run = false;
        }
    }

    public void MemberInformation()
    {
        if (background1 != null) background1.SetActive(false);
        if (background2 != null) background2.SetActive(false);
        if (background3 != null) background3.SetActive(false);

        if (string.IsNullOrEmpty(datatel))
        {
            Debug.LogError("datatel 값이 설정되지 않았습니다.");
            return;
        }

        string tel = datatel.Replace("-", "").Replace(" ", "");
        tel = tel.StartsWith("0") ? tel.Substring(1) : tel;

        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        using (OracleConnection connection = new OracleConnection(connString))
        {
            try
            {
                connection.Open();
                string sql = $"SELECT NAME, BIRTH, SEX, TEL, EMAIL, PHOTO FROM MEMBER WHERE TEL = '{tel}'";
                OracleCommand command = new OracleCommand(sql, connection);
                using (OracleDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (username != null) username.text = ReadString(reader["NAME"]);
                        if (birth != null && reader["BIRTH"] != DBNull.Value)
                        {
                            // DB에서 DateTime으로 읽어와 "yyyy-MM-dd" 형식으로 변환
                            DateTime birthDate = reader.GetDateTime(reader.GetOrdinal("BIRTH"));
                            birth.text = birthDate.ToString("yyyy-MM-dd");
                        }
                        else if (birth != null)
                        {
                            birth.text = string.Empty;
                        }
                        if (sex != null) sex.text = ReadString(reader["SEX"]) == "M" ? "남성" : "여성";
                        if (this.tel != null) this.tel.text = datatel;
                        if (email != null) email.text = ReadString(reader["EMAIL"]);

                        if (photo != null && reader["PHOTO"] != DBNull.Value)
                        {
                            byte[] imageData = (byte[])reader["PHOTO"];
                            if (imageData != null && imageData.Length > 0)
                            {
                                Texture2D texture = new Texture2D(2, 2);
                                if (texture.LoadImage(imageData))
                                {
                                    photo.texture = texture;
                                    photo.color = Color.white;
                                }
                                else
                                {
                                    Debug.LogError("이미지 데이터 로드 실패: byte[]가 유효한 이미지 형식이 아닙니다.");
                                    photo.texture = null;
                                }
                            }
                            else
                            {
                                photo.texture = null;
                            }
                        }
                        else if (photo != null)
                        {
                            photo.texture = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Database connection failed: " + ex.Message);
            }
            finally
            {
                Debug.Log("Database connection closed.");
            }
        }
    }

    public void MemberModify()
    {
        if (background1 != null) background1.SetActive(false);
        if (background2 != null) background2.SetActive(false);
        if (background3 != null) background3.SetActive(false);

        if (string.IsNullOrEmpty(datatel))
        {
            Debug.LogError("datatel 값이 설정되지 않았습니다.");
            return;
        }

        string tel = datatel.Replace("-", "").Replace(" ", "");
        tel = tel.StartsWith("0") ? tel.Substring(1) : tel;

        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        using (OracleConnection connection = new OracleConnection(connString))
        {
            try
            {
                connection.Open();
                string sql = $"SELECT NAME, BIRTH, SEX, TEL, EMAIL, PHOTO FROM MEMBER WHERE TEL = '{tel}'";
                OracleCommand command = new OracleCommand(sql, connection);
                using (OracleDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (username2 != null) username2.text = ReadString(reader["NAME"]);
                        if (birth2 != null && reader["BIRTH"] != DBNull.Value)
                        {
                            // DB에서 DateTime으로 읽어와 "yyyy-MM-dd" 형식으로 변환
                            DateTime birthDate = reader.GetDateTime(reader.GetOrdinal("BIRTH"));
                            birth2.text = birthDate.ToString("yyyy-MM-dd");
                        }
                        else if (birth2 != null)
                        {
                            birth2.text = string.Empty;
                        }
                        if (sex2 != null) sex2.text = ReadString(reader["SEX"]) == "M" ? "남성" : "여성";
                        if (this.tel2 != null) this.tel2.text = datatel;
                        if (email2 != null) email2.text = ReadString(reader["EMAIL"]);

                        if (photo2 != null && reader["PHOTO"] != DBNull.Value)
                        {
                            byte[] imageData = (byte[])reader["PHOTO"];
                            if (imageData != null && imageData.Length > 0)
                            {
                                Texture2D texture = new Texture2D(2, 2);
                                if (texture.LoadImage(imageData))
                                {
                                    photo2.texture = texture;
                                    photo2.color = Color.white;
                                }
                                else
                                {
                                    Debug.LogError("이미지 데이터 로드 실패: byte[]가 유효한 이미지 형식이 아닙니다.");
                                    photo2.texture = null;
                                }
                            }
                            else
                            {
                                photo2.texture = null;
                            }
                        }
                        else if (photo2 != null)
                        {
                            photo2.texture = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Database connection failed: " + ex.Message);
            }
            finally
            {
                Debug.Log("Database connection closed.");

                if (tel2 != null) savetel = tel2.text;
                if (email2 != null) saveemail = email2.text;
            }
        }
    }

    public void DataBaseConnection()
    {
        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        using (OracleConnection connection = new OracleConnection(connString))
        {
            try
            {
                connection.Open();
                OracleCommand command = new OracleCommand("UPDATE_MEMBER", connection);
                command.CommandType = CommandType.StoredProcedure;

                if (username2 == null || string.IsNullOrWhiteSpace(username2.text) ||
                    birth2 == null || string.IsNullOrWhiteSpace(birth2.text) ||
                    sex2 == null || string.IsNullOrWhiteSpace(sex2.text) ||
                    tel2 == null || string.IsNullOrWhiteSpace(tel2.text))
                {
                    if (alertText != null) alertText.text = "필수 입력 항목이 비어 있습니다.";
                    if (alertUI != null) alertUI.SetActive(true);
                    return;
                }

                byte[] photo = null;
                if (this.photo2 != null && this.photo2.texture != null)
                {
                    Texture2D texture = this.photo2.texture as Texture2D;
                    if (texture != null)
                    {
                        photo = texture.EncodeToPNG();
                    }
                }

                DateTime birth;
                if (!DateTime.TryParse(this.birth2.text, out birth))
                {
                    if (failText != null) failText.text = "생년월일 입력 형식이\n잘못되었습니다.";
                    if (failUI != null) failUI.SetActive(true);
                    return;
                }

                char sex;
                string sexInput = this.sex2.text.Trim().ToUpper();
                if (sexInput.StartsWith("M") || sexInput.StartsWith("남"))
                {
                    sex = 'M';
                }
                else if (sexInput.StartsWith("F") || sexInput.StartsWith("W") || sexInput.StartsWith("여"))
                {
                    sex = 'F';
                }
                else
                {
                    if (failText != null) failText.text = "성별 입력 형식이\n잘못되었습니다.";
                    if (failUI != null) failUI.SetActive(true);
                    return;
                }

                string tel = this.tel2.text.Replace("-", "").Replace(" ", "");
                tel = tel.StartsWith("0") ? tel.Substring(1) : tel;

                using (OracleCommand telselect = new OracleCommand($"SELECT COUNT(*) FROM MEMBER WHERE TEL = '{tel}'", connection))
                {
                    if (Convert.ToInt32(telselect.ExecuteScalar()) > 0)
                    {
                        if (failText != null) failText.text = "이미 등록된\n전화번호입니다.";
                        if (failUI != null) failUI.SetActive(true);
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(email2.text))
                {
                    using (OracleCommand emailselect = new OracleCommand($"SELECT COUNT(*) FROM MEMBER WHERE EMAIL = '{email2.text.Trim()}'", connection))
                    {
                        if (Convert.ToInt32(emailselect.ExecuteScalar()) > 0)
                        {
                            if (failText != null) failText.text = "이미 등록된\n이메일입니다.";
                            if (failUI != null) failUI.SetActive(true);
                            return;
                        }
                    }
                }

                command.Parameters.Add("M_NAME", OracleDbType.NVarchar2).Value = username2.text;
                command.Parameters.Add("M_BIRTH", OracleDbType.Date).Value = birth;
                command.Parameters.Add("M_SEX", OracleDbType.Char).Value = sex;
                command.Parameters.Add("M_TEL", OracleDbType.Varchar2).Value = tel;
                command.Parameters.Add("M_EMAIL", OracleDbType.Varchar2).Value = (string.IsNullOrEmpty(email2.text)) ? DBNull.Value : email2.text;
                command.Parameters.Add("M_PHOTO", OracleDbType.Blob).Value = (photo == null) ? DBNull.Value : photo;

                command.ExecuteNonQuery();

                if (successUI != null) successUI.SetActive(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"DB 등록/수정 중 오류 발생: {ex.Message}");
                if (alertText != null) alertText.text = "등록/수정 중 오류가 발생했습니다.\n" + ex.Message;
                if (alertUI != null) alertUI.SetActive(true);
            }
            finally
            {
                Debug.Log("Database connection closed.");
                if (tableController != null)
                {
                    tableController.RefreshTable();
                }
            }
        }
    }

    public void MemberUpdate()
    {
        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        using (OracleConnection connection = new OracleConnection(connString))
        {
            try
            {
                connection.Open();
                OracleCommand command = new OracleCommand("UPDATE_MEMBER", connection);
                command.CommandType = CommandType.StoredProcedure;

                string cleanSavetel = savetel.Replace("-", "").Replace(" ", "");
                cleanSavetel = cleanSavetel.StartsWith("0") ? cleanSavetel.Substring(1) : cleanSavetel;

                string sql = $"SELECT MNO FROM MEMBER WHERE TEL = '{cleanSavetel}'";
                OracleCommand mnoCommand = new OracleCommand(sql, connection);
                object mnoResult = mnoCommand.ExecuteScalar();

                decimal? memberMno = null;
                if (mnoResult != null && mnoResult != DBNull.Value)
                {
                    memberMno = Convert.ToDecimal(mnoResult);
                }

                if (memberMno == null)
                {
                    Debug.LogError("수정할 멤버의 MNO를 찾을 수 없습니다.");
                    if (failText != null) failText.text = "수정할 회원 정보를 찾을 수 없습니다.";
                    if (failUI != null) failUI.SetActive(true);
                    return;
                }

                if (username2 == null || string.IsNullOrWhiteSpace(username2.text) ||
                    birth2 == null || string.IsNullOrWhiteSpace(birth2.text) ||
                    sex2 == null || string.IsNullOrWhiteSpace(sex2.text) ||
                    tel2 == null || string.IsNullOrWhiteSpace(tel2.text))
                {
                    if (alertText != null) alertText.text = "필수 입력 항목이 비어 있습니다.";
                    if (alertUI != null) alertUI.SetActive(true);
                    return;
                }

                byte[] photo = null;
                if (this.photo2 != null && this.photo2.texture != null)
                {
                    Texture2D texture = this.photo2.texture as Texture2D;
                    if (texture != null)
                    {
                        photo = texture.EncodeToPNG();
                    }
                }

                DateTime birth;
                if (!DateTime.TryParse(this.birth2.text, out birth))
                {
                    if (failText != null) failText.text = "생년월일 입력 형식이\n잘못되었습니다.";
                    if (failUI != null) failUI.SetActive(true);
                    return;
                }

                char sex;
                string sexInput = this.sex2.text.Trim().ToUpper();
                if (sexInput.StartsWith("M") || sexInput.StartsWith("남"))
                {
                    sex = 'M';
                }
                else if (sexInput.StartsWith("F") || sexInput.StartsWith("W") || sexInput.StartsWith("여"))
                {
                    sex = 'F';
                }
                else
                {
                    if (failText != null) failText.text = "성별 입력 형식이\n잘못되었습니다.";
                    if (failUI != null) failUI.SetActive(true);
                    return;
                }

                string tel = this.tel2.text.Replace("-", "").Replace(" ", "");
                tel = tel.StartsWith("0") ? tel.Substring(1) : tel;

                if (cleanSavetel != tel)
                {
                    using (OracleCommand telselect = new OracleCommand($"SELECT COUNT(*) FROM MEMBER WHERE TEL = '{tel}' AND MNO != {memberMno.Value}", connection))
                    {
                        if (Convert.ToInt32(telselect.ExecuteScalar()) > 0)
                        {
                            if (failText != null) failText.text = "이미 등록된\n전화번호입니다.";
                            if (failUI != null) failUI.SetActive(true);
                            return;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(email2.text) && saveemail != email2.text)
                {
                    using (OracleCommand emailselect = new OracleCommand($"SELECT COUNT(*) FROM MEMBER WHERE EMAIL = '{email2.text.Trim()}' AND MNO != {memberMno.Value}", connection))
                    {
                        if (Convert.ToInt32(emailselect.ExecuteScalar()) > 0)
                        {
                            if (failText != null) failText.text = "이미 등록된\n이메일입니다.";
                            if (failUI != null) failUI.SetActive(true);
                            return;
                        }
                    }
                }

                command.Parameters.Add("M_MNO", OracleDbType.Decimal).Value = memberMno.Value;
                command.Parameters.Add("M_NAME", OracleDbType.NVarchar2).Value = username2.text;
                command.Parameters.Add("M_BIRTH", OracleDbType.Date).Value = birth;
                command.Parameters.Add("M_SEX", OracleDbType.Char).Value = sex;
                command.Parameters.Add("M_TEL", OracleDbType.Varchar2).Value = tel;
                command.Parameters.Add("M_EMAIL", OracleDbType.Varchar2).Value = (string.IsNullOrEmpty(email2.text)) ? DBNull.Value : email2.text;
                command.Parameters.Add("M_PHOTO", OracleDbType.Blob).Value = (photo == null) ? DBNull.Value : photo;

                command.ExecuteNonQuery();

                if (successUI != null) successUI.SetActive(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"DB 수정 중 오류 발생: {ex.Message}");
                if (alertText != null) alertText.text = "수정 중 오류가 발생했습니다.\n" + ex.Message;
                if (alertUI != null) alertUI.SetActive(true);
                if (successUI != null) successUI.SetActive(false);
            }
            finally
            {
                Debug.Log("Database connection closed.");
                if (tableController != null)
                {
                    tableController.RefreshTable();
                }
            }
        }
    }

    public void MemberDelete()
    {
        Debug.Log($"삭제(Soft Delete) 시도: {dataname} ({datatel})");

        if (background1 != null) background1.SetActive(false);
        if (background2 != null) background2.SetActive(false);
        if (background3 != null) background3.SetActive(false);

        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        using (OracleConnection connection = new OracleConnection(connString))
        {
            if (string.IsNullOrEmpty(datatel))
            {
                Debug.LogError("삭제할 회원 전화번호(datatel)가 설정되지 않았습니다.");
                if (alertText != null) alertText.text = "삭제할 회원 정보가 없습니다.";
                if (alertUI != null) alertUI.SetActive(true);
                delete = false;
                return;
            }

            string tel = datatel.Replace("-", "").Replace(" ", "");
            tel = tel.StartsWith("0") ? tel.Substring(1) : tel;

            try
            {
                connection.Open();

                // 1. MNO 조회
                string selectMnoSql = "SELECT MNO FROM MEMBER WHERE TEL = :tel";
                OracleCommand selectMnoCommand = new OracleCommand(selectMnoSql, connection);
                selectMnoCommand.Parameters.Add("tel", OracleDbType.Varchar2).Value = tel; // SQL Injection 방지를 위해 파라미터 사용
                object mnoResult = selectMnoCommand.ExecuteScalar();

                if (mnoResult == null || mnoResult == DBNull.Value)
                {
                    if (alertText != null) alertText.text = "삭제할 회원 정보를 찾을 수 없습니다.";
                    if (alertUI != null) alertUI.SetActive(true);
                    return;
                }
                decimal mno = Convert.ToDecimal(mnoResult);

                // --- 🌟 추가된 조건문 시작: 대여 중인 도서 확인 (IS_RETURNED = 'N') ---
                string checkRentSql = "SELECT COUNT(*) FROM RENT WHERE MNO = :mno AND IS_RETURNED = 'N'";
                OracleCommand checkRentCommand = new OracleCommand(checkRentSql, connection);
                checkRentCommand.Parameters.Add("mno", OracleDbType.Decimal).Value = mno;

                int rentCount = Convert.ToInt32(checkRentCommand.ExecuteScalar());

                if (rentCount > 0)
                {
                    // 대여 중인 도서가 하나라도 있을 경우, failDelete 팝업을 띄움
                    Debug.LogWarning($"MNO {mno}는 반납되지 않은 도서 {rentCount}권이 있어 삭제할 수 없습니다.");

                    // 🌟 텍스트 설정 로직 (if (failDeleteText != null) ...)은 제거합니다.

                    // 🌟 failDeleteUI만 활성화합니다. (팝업 자체에 이미 실패 메시지가 디자인되어 있다고 가정)
                    if (failDeleteUI != null)
                        failDeleteUI.SetActive(true);

                    return;
                }
                // --- 🌟 추가된 조건문 종료 ---

                // 2. Soft Delete (대여 중인 도서가 없을 경우에만 실행)
                string updateSql = "UPDATE MEMBER SET DELETE_AT = SYSDATE WHERE MNO = :mno";
                OracleCommand command = new OracleCommand(updateSql, connection);
                command.Parameters.Add("mno", OracleDbType.Decimal).Value = mno;

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    Debug.Log($"MNO {mno}의 DELETE_AT 업데이트 완료 (Soft Delete)");
                    if (deleteUI != null) deleteUI.SetActive(true); // 삭제 성공 팝업

                    if (tableController != null)
                    {
                        tableController.RefreshTable();
                    }
                }
                else
                {
                    if (alertText != null) alertText.text = "회원 정보 업데이트(삭제) 실패.";
                    if (alertUI != null) alertUI.SetActive(true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"DB Soft Delete 실패: {ex.Message}");
                if (alertText != null) alertText.text = $"DB Soft Delete 실패: \n{ex.Message}";
                if (alertUI != null) alertUI.SetActive(true);
                if (deleteUI != null) deleteUI.SetActive(false);
            }
            finally
            {
                delete = false;
                Debug.Log("Database connection closed.");
            }
        }
    }

    private string ReadString(object dbValue)
    {
        return (dbValue == DBNull.Value || dbValue == null) ? string.Empty : dbValue.ToString();
    }

    public void SceneInit()
    {
    }
}